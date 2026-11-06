using System.Globalization;

namespace Yamori.Core.Settings;

/// <summary>
/// <see cref="AppLanguage"/> と <see cref="CultureInfo"/> の対応。
/// </summary>
public static class AppLanguageCultures
{
    /// <summary>
    /// 言語の選択肢に対応するカルチャ名を取得する。
    /// </summary>
    /// <param name="language">言語の選択肢。</param>
    /// <returns>カルチャ名（例：<c>ja-JP</c>）。<see cref="AppLanguage.System"/> のときは null。</returns>
    public static string? GetCultureName(AppLanguage language) =>
        language switch
        {
            AppLanguage.Japanese => "ja-JP",
            AppLanguage.English => "en-US",
            AppLanguage.Spanish => "es-ES",
            _ => null,
        };

    /// <summary>
    /// 言語の選択肢に対応するカルチャを取得する。
    /// </summary>
    /// <param name="language">言語の選択肢。</param>
    /// <param name="systemCulture"><see cref="AppLanguage.System"/> のときに使う、Windows の表示言語のカルチャ。</param>
    /// <returns>使うカルチャ。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="systemCulture"/> が null。</exception>
    public static CultureInfo Resolve(AppLanguage language, CultureInfo systemCulture)
    {
        ArgumentNullException.ThrowIfNull(systemCulture);
        return GetCultureName(language) is { } name ? CultureInfo.GetCultureInfo(name) : systemCulture;
    }
}
