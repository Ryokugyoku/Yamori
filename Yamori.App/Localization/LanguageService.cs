using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Yamori.Core.Settings;

namespace Yamori.Localization;

/// <summary>
/// 画面の表示言語を切り替える。
/// </summary>
public sealed class LanguageService
{
    private readonly CultureInfo _systemUiCulture;
    private readonly CultureInfo _systemCulture;

    /// <summary>
    /// 起動時のカルチャ（Windows の表示言語）を覚えて作る。言語を変える前に作ること。
    /// </summary>
    public LanguageService()
    {
        _systemUiCulture = CultureInfo.CurrentUICulture;
        _systemCulture = CultureInfo.CurrentCulture;
    }

    /// <summary>
    /// 言語を切り替える。以後に作る画面と、別スレッドで作る文字列に反映される。
    /// </summary>
    /// <param name="language">言語の選択肢。</param>
    public void Apply(AppLanguage language)
    {
        var uiCulture = AppLanguageCultures.Resolve(language, _systemUiCulture);
        var culture = language == AppLanguage.System ? _systemCulture : uiCulture;

        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;
        CultureInfo.CurrentCulture = culture;

        try
        {
            // WinUI 標準コントロールの文字列（日付の選択など）も同じ言語にする。
            Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride =
                AppLanguageCultures.GetCultureName(language) ?? string.Empty;
        }
        catch (COMException)
        {
            // 実行環境によっては設定できない。Yamori 自身の文字列は上のカルチャで切り替わるため、続行する。
        }
    }
}
