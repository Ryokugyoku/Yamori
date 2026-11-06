using Yamori.Core.Settings;

namespace Yamori.ViewModels;

/// <summary>
/// 言語の選択肢 1 件分の表示内容。
/// </summary>
/// <param name="Language">言語。</param>
/// <param name="DisplayName">選択肢に表示する名前。各言語はその言語自身の名前で表示する。</param>
public sealed record LanguageOption(AppLanguage Language, string DisplayName)
{
    /// <summary>
    /// ComboBox に表示する文字列を返す。
    /// </summary>
    /// <returns>表示名。</returns>
    public override string ToString() => DisplayName;
}
