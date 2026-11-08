using System;
using System.Threading;
using System.Threading.Tasks;
using Yamori.Core.Notifications;
using Yamori.Core.Storage.Legacy;
using Yamori.ViewModels;
using Yamori.Views;

namespace Yamori.Composition;

/// <summary>
/// 起動からサインイン、メイン画面の表示、言語を変えたときの画面の作り直しまでの流れを受け持つ。
/// </summary>
public sealed class AppSession : IDisposable
{
    private readonly AppServices _services;
    private readonly MainWindow _window;
    /// <summary>終了するときに、Helper との接続と代わりの表示が止まるのを待つ最長の時間。</summary>
    private static readonly TimeSpan SensorsStopTimeout = TimeSpan.FromSeconds(2);

    private readonly CancellationTokenSource _monitorCancellation = new();
    private MainViewModel? _main;
    private HelperStatusViewModel? _status;
    private Task? _sensorsRun;
    private ShellView? _shell;
    private NotificationActivationRequest? _pendingActivation;

    /// <summary>
    /// 部品とウィンドウを指定して作る。
    /// </summary>
    /// <param name="services">組み立て済みの部品。</param>
    /// <param name="window">表示先のウィンドウ。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public AppSession(AppServices services, MainWindow window)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(window);
        _services = services;
        _window = window;
    }

    /// <summary>
    /// サインイン画面を表示する。サインインが済むとメイン画面に切り替わる。
    /// </summary>
    public void Start()
    {
        // 以前の版が残した使用率の記録（CSV）を消す。記録は Helper だけが行うため、もう使わない。
        // 設定ファイルに残っている以前の指定フォルダーを読むため、設定を保存し直す前（サインインより前）に始める。
        _ = Task.Run(() => LegacyHistoryCleaner.Clean(LegacyHistoryLocator.CreateDefault().Find()));

        var login = _services.CreateLoginViewModel(() => _window.Handle);
        login.SignedIn += OnSignedIn;
        _window.ShowContent(new LoginView(login));
        _ = login.InitializeAsync();
    }

    /// <summary>
    /// Helper との接続を止め、イベントの購読を解除する。センサーの記録は Helper が続ける。
    /// </summary>
    public void Dispose()
    {
        _monitorCancellation.Cancel();
        try
        {
            // 止めるときのまとめ書きが終わるまで、少しだけ待つ。
            _sensorsRun?.Wait(SensorsStopTimeout);
        }
        catch (AggregateException)
        {
            // 止める途中の失敗は、終了の妨げにしない。
        }

        _main?.Dispose();
        _status?.Dispose();
        _services.Sensors.Dispose();

        _monitorCancellation.Dispose();
    }

    private void OnSignedIn(object? sender, EventArgs e)
    {
        if (sender is LoginViewModel login)
        {
            login.SignedIn -= OnSignedIn;
        }

        // Helper への接続はサインインした後に始める（記録は Helper が画面と関係なく続けている）。
        var token = _monitorCancellation.Token;
        _sensorsRun = Task.Run(() => _services.Sensors.RunAsync(token), token);

        _main = _services.CreateMainViewModel();
        ShowShell(openSettings: false);
        _ = _main.RefreshCommand.ExecuteAsync(null);

        // サインイン前に通知を押して開いたときは、ここでその画面へ移る。
        if (_pendingActivation is { } pending)
        {
            _pendingActivation = null;
            ApplyActivation(pending);
        }
    }

    /// <summary>
    /// 通知を押してアプリが開かれたときの指定に従って画面を切り替える。
    /// サインインがまだなら覚えておき、サインインのあとで切り替える（通知から開いても、サインインは省かない）。UI スレッドから呼ぶこと。
    /// </summary>
    /// <param name="request">開き方の指定。</param>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> が null。</exception>
    public void HandleActivation(NotificationActivationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_shell is null)
        {
            _pendingActivation = request;
            return;
        }

        ApplyActivation(request);
    }

    private void ApplyActivation(NotificationActivationRequest request)
    {
        if (request.Kind != NotificationActivationKind.OpenAnomaly)
        {
            return;
        }

        // 段階 4 で「分析」画面ができたら、そこでこの異常（request.AnomalyId）を選んだ状態で開く。
        // それまでは、通知から開いたことが分かるようにリアルタイム画面を開く。
        _shell?.SelectPage("Realtime");
    }

    private void ShowShell(bool openSettings)
    {
        // タイトルバーの見守りの状態も、表示言語に合わせて作り直す。
        _status?.Dispose();
        _status = _services.CreateHelperStatusViewModel();
        _window.ShowTitleBarStatus(new HelperStatusIndicator(_status));

        var settings = _services.CreateSettingsViewModel();
        settings.LanguageChanged += OnLanguageChanged;
        _shell = new ShellView(_main!, settings, _services.CreateRealtimeViewModel, openSettings);
        _window.ShowContent(_shell);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (sender is SettingsViewModel settings)
        {
            settings.LanguageChanged -= OnLanguageChanged;
        }

        // 文字列は画面を作った時点の言語で決まるため、画面ごと作り直す。
        ShowShell(openSettings: true);
        _ = _main!.ReloadForLanguageAsync();
    }
}
