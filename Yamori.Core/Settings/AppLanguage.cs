namespace Yamori.Core.Settings;

/// <summary>
/// 画面に表示する言語の選択肢。
/// </summary>
public enum AppLanguage
{
    /// <summary>Windows の表示言語に合わせる。対応していない言語のときは英語で表示する。</summary>
    System = 0,

    /// <summary>日本語。</summary>
    Japanese = 1,

    /// <summary>英語。</summary>
    English = 2,

    /// <summary>スペイン語。</summary>
    Spanish = 3,
}
