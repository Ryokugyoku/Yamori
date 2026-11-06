using Microsoft.UI.Xaml.Markup;

namespace Yamori.Localization;

/// <summary>
/// XAML から翻訳済みの文字列を参照するマークアップ拡張。使い方：<c>Text="{l:Loc Key=Nav_Overview}"</c>。
/// </summary>
/// <remarks>
/// 値は画面を作った時点の言語で決まる。言語を変えたときは画面を作り直す。
/// </remarks>
[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed partial class Loc : MarkupExtension
{
    /// <summary>
    /// リソースのキーを取得または設定する。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// キーに対応する文字列を返す。
    /// </summary>
    /// <returns>翻訳された文字列。</returns>
    protected override object ProvideValue() => UiText.Get(Key);
}
