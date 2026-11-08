namespace Yamori.Core.Anomalies.Detection;

/// <summary>
/// 続いている異常 1 つの、<see cref="AnomalyDetector"/> のメモリ上の状態。
/// </summary>
/// <param name="record">今の異常。</param>
/// <param name="persistedUtc">最後にマスタに書いた時刻。</param>
internal sealed class OpenAnomaly(AnomalyRecord record, DateTimeOffset persistedUtc)
{
    /// <summary>今の異常を取得または設定する。</summary>
    public AnomalyRecord Record { get; set; } = record;

    /// <summary>最後にマスタに書いた時刻を取得または設定する。</summary>
    public DateTimeOffset PersistedUtc { get; set; } = persistedUtc;
}
