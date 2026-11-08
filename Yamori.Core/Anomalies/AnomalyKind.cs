namespace Yamori.Core.Anomalies;

/// <summary>
/// 異常の種類。
/// </summary>
/// <remarks>
/// マスタと IPC で数値として扱うため、既存の値の番号は変えないこと。追加は末尾に行う。
/// </remarks>
public enum AnomalyKind
{
    /// <summary>サーマルスロットリング（熱で性能を落としている）。</summary>
    ThermalThrottling = 0,

    /// <summary>ふだんより悪い（待機中の温度・負荷が、自分の過去 14 日の同じくらいの負荷の時間より高い）。</summary>
    WorseThanUsual = 1,
}
