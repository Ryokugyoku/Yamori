namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 時系列の解像度。RocksDB ではそれぞれ別の Column Family に置く。
/// </summary>
/// <remarks>
/// IPC で数値として送るため、既存の値の番号は変えないこと。追加は末尾に行う。
/// </remarks>
public enum TimeSeriesResolution
{
    /// <summary>生データ（Column Family <c>raw</c>）。</summary>
    Raw = 0,

    /// <summary>1 分ごとの集計（Column Family <c>rollup_1m</c>）。</summary>
    OneMinute = 1,

    /// <summary>1 時間ごとの集計（Column Family <c>rollup_1h</c>）。</summary>
    OneHour = 2,

    /// <summary>
    /// 1 秒ごとの値（Column Family <c>fine_1s</c>）。異常の検出と分析に使う一部のセンサーだけを、記録の設定とは別に毎秒残す（版 3 から）。
    /// 集計ではなく、生データと同じ形の点。
    /// </summary>
    OneSecond = 3,
}
