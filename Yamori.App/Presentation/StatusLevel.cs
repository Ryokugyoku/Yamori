namespace Yamori.Presentation;

/// <summary>
/// 状態の札（<see cref="Controls.StatusChip"/>）の色分け。不安をあおらないよう、赤は本当に対応が必要なときだけ使う。
/// </summary>
public enum StatusLevel
{
    /// <summary>中立（判定できない、データが足りないなど）。灰色。</summary>
    Neutral,

    /// <summary>お知らせ（処理中など）。青。</summary>
    Attention,

    /// <summary>問題なし。緑。</summary>
    Good,

    /// <summary>気にしておくとよい。黄。</summary>
    Caution,

    /// <summary>対応が必要。赤。</summary>
    Critical,
}
