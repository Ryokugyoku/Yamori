using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Yamori.Accounts;
using Yamori.Core.Settings;
using Yamori.Localization;
using Yamori.Presentation;
using Yamori.Services;

namespace Yamori.ViewModels;

/// <summary>
/// 設定画面の状態。表示言語と異常の通知のオン・オフを変更し、記録の扱い（Helper が PC の中に保存すること）を説明する。
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly LanguageService _languageService;

    private YamoriSettings _settings;

    /// <summary>
    /// 必要な処理と今の設定を指定して作る。
    /// </summary>
    /// <param name="settingsStore">設定の保存先。</param>
    /// <param name="settings">今の設定。</param>
    /// <param name="languageService">表示言語を切り替える処理。</param>
    /// <param name="account">サインイン中のアカウント。</param>
    /// <param name="sensorDataDirectory">Helper が記録を保存するフォルダー（画面に表示する）。</param>
    /// <param name="notifications">異常の通知の設定（Helper に保存する）。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public SettingsViewModel(
        ISettingsStore settingsStore,
        YamoriSettings settings,
        LanguageService languageService,
        WindowsAccount account,
        string sensorDataDirectory,
        NotificationSettingsViewModel notifications)
    {
        ArgumentNullException.ThrowIfNull(settingsStore);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(languageService);
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(sensorDataDirectory);
        ArgumentNullException.ThrowIfNull(notifications);

        _settingsStore = settingsStore;
        _settings = settings;
        _languageService = languageService;

        Account = account;
        Notifications = notifications;
        DataDirectoryText = UiText.Format("Storage_PathLabel", sensorDataDirectory);
        Languages =
        [
            new(AppLanguage.System, UiText.Get("Settings_LanguageSystem")),
            new(AppLanguage.Japanese, "日本語"),
            new(AppLanguage.English, "English"),
            new(AppLanguage.Spanish, "Español"),
        ];
    }

    /// <summary>
    /// 表示言語が変わったときに発生する。画面を作り直す合図。
    /// </summary>
    public event EventHandler? LanguageChanged;

    /// <summary>サインイン中のアカウントを取得する。</summary>
    public WindowsAccount Account { get; }

    /// <summary>異常の通知の設定を取得する。</summary>
    public NotificationSettingsViewModel Notifications { get; }

    /// <summary>Yamori の版の表示（例：バージョン 1.0.0.0）を取得する。</summary>
    public string VersionText { get; } = UiText.Format("Settings_Version", AppVersion.Get());

    /// <summary>Helper が記録を保存するフォルダーを示す文字列を取得する。</summary>
    public string DataDirectoryText { get; }

    /// <summary>言語の選択肢を取得する。</summary>
    public IReadOnlyList<LanguageOption> Languages { get; }

    /// <summary>
    /// 選ばれている言語の、<see cref="Languages"/> の中での位置を取得または設定する。変えるとすぐに保存し、画面を作り直す。
    /// </summary>
    public int SelectedLanguageIndex
    {
        get => Languages.ToList().FindIndex(o => o.Language == _settings.Language);
        set
        {
            if (value < 0 || value >= Languages.Count || Languages[value].Language == _settings.Language)
            {
                return;
            }

            var language = Languages[value].Language;
            Save(_settings with { Language = language });
            _languageService.Apply(language);
            OnPropertyChanged();
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Save(YamoriSettings next)
    {
        _settings = next;
        try
        {
            _settingsStore.Save(next);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 設定を保存できなくても、今回の起動中は新しい設定で動かす（言語を変えると画面を作り直すため、ここでは知らせない）。
        }
    }
}
