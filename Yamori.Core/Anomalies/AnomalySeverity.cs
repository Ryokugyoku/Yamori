namespace Yamori.Core.Anomalies;

/// <summary>
/// 異常の重さ。大きいほど重い。
/// </summary>
/// <remarks>
/// マスタと IPC で数値として扱うため、既存の値の番号は変えないこと。
/// </remarks>
public enum AnomalySeverity
{
    /// <summary>注意（様子を見てよい）。</summary>
    Notice = 0,

    /// <summary>警告（性能や寿命に響いている）。</summary>
    Warning = 1,

    /// <summary>危険（すぐに対処したほうがよい）。</summary>
    Critical = 2,
}
