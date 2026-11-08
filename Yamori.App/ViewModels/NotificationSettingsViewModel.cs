using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Service;
using Yamori.Core.Sensors.Ipc.Client;
using Yamori.Core.Sensors.Service;
using Yamori.Localization;

namespace Yamori.ViewModels;

/// <summary>
/// 設定画面の「通知」のまとまり。異常の通知の全体と、異常の種類ごとのオン・オフを Helper から読み、変えたら Helper に保存する。
/// 通知専用の常駐プログラムと Helper は、この設定に従う。
/// </summary>
/// <remarks>UI スレッドで作り、UI スレッドから使うこと。画面に出している間だけ <see cref="Activate"/> で Helper の接続を見張る。</remarks>
public sealed class NotificationSettingsViewModel : ObservableObject
{
    /// <summary>画面に出す異常の種類。「ふだんより悪い」は検出を作るとき（段階 5）に足す。</summary>
    private static readonly AnomalyKind[] ShownKinds = [AnomalyKind.ThermalThrottling];

    private readonly IAnomalyService _anomalies;
    private readonly IHelperConnection _connection;
    private readonly SynchronizationContext _uiContext;
    private readonly Dictionary<AnomalyKind, AnomalySetting> _settings = [];
    private bool _isAvailable;
    private bool _isEnabled = true;
    private bool _isLoading;
    private string _statusText = string.Empty;

    /// <summary>
    /// 異常の設定の窓口と、Helper との接続を指定して作る。
    /// </summary>
    /// <param name="anomalies">異常の設定の窓口（Helper へのパイプ）。</param>
    /// <param name="connection">Helper との接続の状態。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    /// <exception cref="InvalidOperationException">UI スレッド以外から呼ばれた。</exception>
    public NotificationSettingsViewModel(IAnomalyService anomalies, IHelperConnection connection)
    {
        ArgumentNullException.ThrowIfNull(anomalies);
        ArgumentNullException.ThrowIfNull(connection);
        _anomalies = anomalies;
        _connection = connection;
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException("NotificationSettingsViewModel は UI スレッドで作成してください。");

        Kinds = ShownKinds
            .Select(kind => new AnomalyKindNotificationOption(
                kind,
                UiText.Get($"Settings_Notify_{kind}"),
                UiText.Get($"Settings_Notify_{kind}_Description"),
                OnKindChanged))
            .ToArray();
        StatusText = UiText.Get("Settings_NotifyLoading");
    }

    /// <summary>異常の種類ごとのオン・オフを取得する。</summary>
    public IReadOnlyList<AnomalyKindNotificationOption> Kinds { get; }

    /// <summary>Helper から設定を読めていて、変えられるかどうかを取得する。</summary>
    public bool IsAvailable
    {
        get => _isAvailable;
        private set
        {
            if (SetProperty(ref _isAvailable, value))
            {
                UpdateKindsEditable();
            }
        }
    }

    /// <summary>
    /// 異常の通知の全体のオン・オフを取得または設定する。画面から変えると Helper に保存する。オフでも異常は一覧に残る。
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (!SetProperty(ref _isEnabled, value))
            {
                return;
            }

            UpdateKindsEditable();
            if (!_isLoading)
            {
                _ = SaveEnabledAsync(value);
            }
        }
    }

    /// <summary>Helper につながっていない、保存できなかったなどの知らせを取得する。なければ空。</summary>
    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (SetProperty(ref _statusText, value))
            {
                OnPropertyChanged(nameof(HasStatus));
            }
        }
    }

    /// <summary>知らせがあるかどうかを取得する。</summary>
    public bool HasStatus => _statusText.Length > 0;

    /// <summary>
    /// 画面に出たときに呼ぶ。Helper から設定を読み、つながり直したら読み直す。
    /// </summary>
    public void Activate()
    {
        _connection.ConnectionStateChanged += OnConnectionStateChanged;
        _ = LoadAsync();
    }

    /// <summary>
    /// 画面から外れたときに呼ぶ。接続の見張りをやめる。
    /// </summary>
    public void Deactivate() => _connection.ConnectionStateChanged -= OnConnectionStateChanged;

    private void OnConnectionStateChanged(object? sender, HelperConnectionState state) =>
        _uiContext.Post(_ => _ = LoadAsync(), null);

    private async Task LoadAsync()
    {
        try
        {
            var enabled = await _anomalies.GetNotificationsEnabledAsync();
            var settings = await _anomalies.GetSettingsAsync();
            if (enabled is not { } isEnabled || settings.Count == 0)
            {
                IsAvailable = false;
                StatusText = UiText.Get("Settings_NotifyUnavailable");
                return;
            }

            _isLoading = true;
            try
            {
                IsEnabled = isEnabled;
                foreach (var setting in settings)
                {
                    _settings[setting.Kind] = setting;
                }

                foreach (var option in Kinds)
                {
                    option.Load(_settings.TryGetValue(option.Kind, out var setting) ? setting.NotificationsEnabled : true);
                }
            }
            finally
            {
                _isLoading = false;
            }

            IsAvailable = true;
            StatusText = string.Empty;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // パイプの想定外の失敗。画面は落とさず、変えられないことだけを示す。
            IsAvailable = false;
            StatusText = UiText.Get("Settings_NotifyUnavailable");
        }
    }

    private async Task SaveEnabledAsync(bool enabled)
    {
        var result = await TrySaveAsync(() => _anomalies.SetNotificationsEnabledAsync(enabled));
        if (result != SensorSettingResult.Applied)
        {
            await LoadAsync();
            StatusText = UiText.Get("Settings_NotifySaveFailed");
        }
    }

    private void OnKindChanged(AnomalyKindNotificationOption option, bool isOn)
    {
        if (!_isLoading)
        {
            _ = SaveKindAsync(option, isOn);
        }
    }

    private async Task SaveKindAsync(AnomalyKindNotificationOption option, bool isOn)
    {
        // しきい値は変えない（手動のしきい値は、分析画面を作るときに設定画面へ足す）。
        var current = _settings.TryGetValue(option.Kind, out var setting) ? setting : AnomalySettingRules.Default(option.Kind);
        var next = current with { NotificationsEnabled = isOn };
        var result = await TrySaveAsync(() => _anomalies.SetSettingAsync(next));
        if (result == SensorSettingResult.Applied)
        {
            _settings[option.Kind] = next;
            return;
        }

        await LoadAsync();
        StatusText = UiText.Get("Settings_NotifySaveFailed");
    }

    private static async Task<SensorSettingResult> TrySaveAsync(Func<Task<SensorSettingResult>> save)
    {
        try
        {
            return await save();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return SensorSettingResult.Unavailable;
        }
    }

    private void UpdateKindsEditable()
    {
        foreach (var option in Kinds)
        {
            option.IsEditable = _isAvailable && _isEnabled;
        }
    }
}
