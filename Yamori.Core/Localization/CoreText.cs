using System.Globalization;
using System.Resources;

namespace Yamori.Core.Localization;

/// <summary>
/// Yamori.Core の表示文字列を、現在の UI カルチャ（<see cref="CultureInfo.CurrentUICulture"/>）で取り出す。
/// </summary>
/// <remarks>
/// 文字列の本体は <c>Resources/CoreStrings*.resx</c> にある。既定（ニュートラル）は英語で、日本語・スペイン語はサテライトアセンブリ。
/// </remarks>
internal static class CoreText
{
    private static readonly ResourceManager Resources = new("Yamori.Core.Resources.CoreStrings", typeof(CoreText).Assembly);

    /// <summary>
    /// キーに対応する文字列を取得する。
    /// </summary>
    /// <param name="key">リソースのキー。</param>
    /// <returns>翻訳された文字列。キーが見つからなければ <c>[key]</c>。</returns>
    public static string Get(string key) =>
        Resources.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]";

    /// <summary>
    /// キーに対応する書式文字列に値を埋め込む。数値の書式も現在の UI カルチャに従う。
    /// </summary>
    /// <param name="key">リソースのキー。</param>
    /// <param name="args">埋め込む値。</param>
    /// <returns>値を埋め込んだ文字列。</returns>
    public static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentUICulture, Get(key), args);
}
