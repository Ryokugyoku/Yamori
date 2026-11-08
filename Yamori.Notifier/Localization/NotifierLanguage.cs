using System.Globalization;
using Yamori.Core.Settings;
using Yamori.Core.Storage;

namespace Yamori.Notifier.Localization;

/// <summary>
/// アプリ本体の設定（settings.json）の表示言語を、通知専用の常駐プログラムにも当てはめる。
/// </summary>
internal static class NotifierLanguage
{
    /// <summary>
    /// 設定の表示言語を読み、このプロセスの UI カルチャにする。設定が読めなければ Windows の表示言語のまま。
    /// </summary>
    public static void Apply()
    {
        var language = new JsonSettingsStore(YamoriDataPaths.GetSettingsFilePath()).Load().Language;
        var uiCulture = AppLanguageCultures.Resolve(language, CultureInfo.CurrentUICulture);

        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
        CultureInfo.CurrentUICulture = uiCulture;
    }
}
