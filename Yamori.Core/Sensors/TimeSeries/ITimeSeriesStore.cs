namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 時系列のログ（生データと集計）を永続化するストア。今は RocksDB で実装し、ほかの実装に差し替えられるようにこの形で使う。
/// </summary>
/// <remarks>時刻はすべて UTC の Unix ミリ秒。実装はスレッドセーフであること。</remarks>
public interface ITimeSeriesStore
{
    /// <summary>
    /// 生データをまとめて書き込む（1 回の書き込みにまとめる）。同じセンサー・同じ時刻の点は上書きする。
    /// </summary>
    /// <param name="points">書き込む点。</param>
    /// <exception cref="ArgumentNullException"><paramref name="points"/> が null。</exception>
    void WriteRaw(IReadOnlyCollection<TimeSeriesPoint> points);

    /// <summary>
    /// 集計をまとめて書き込む。同じセンサー・同じ区間の集計は上書きする（集計のやり直しに使う）。
    /// </summary>
    /// <param name="resolution">集計の解像度。</param>
    /// <param name="points">書き込む区間。</param>
    /// <exception cref="ArgumentNullException"><paramref name="points"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="resolution"/> が生データ。</exception>
    void WriteRollups(TimeSeriesResolution resolution, IReadOnlyCollection<RollupPoint> points);

    /// <summary>
    /// センサー 1 つの生データを、時刻の範囲で古い順に読む。
    /// </summary>
    /// <param name="sensorId">センサーの番号。</param>
    /// <param name="fromMs">範囲の始まり（含む）。</param>
    /// <param name="toMsExclusive">範囲の終わり（含まない）。</param>
    /// <returns>範囲内の点。</returns>
    IReadOnlyList<TimeSeriesPoint> ReadRaw(uint sensorId, long fromMs, long toMsExclusive);

    /// <summary>
    /// センサー 1 つの集計を、区間の始まりの範囲で古い順に読む。
    /// </summary>
    /// <param name="resolution">集計の解像度。</param>
    /// <param name="sensorId">センサーの番号。</param>
    /// <param name="fromMs">範囲の始まり（含む）。</param>
    /// <param name="toMsExclusive">範囲の終わり（含まない）。</param>
    /// <returns>範囲内の区間。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="resolution"/> が生データ。</exception>
    IReadOnlyList<RollupPoint> ReadRollups(TimeSeriesResolution resolution, uint sensorId, long fromMs, long toMsExclusive);

    /// <summary>
    /// 指定したセンサーの、指定した時刻より前のデータを消す（RocksDB では DeleteRange）。
    /// </summary>
    /// <param name="resolution">解像度。</param>
    /// <param name="sensorIds">対象のセンサーの番号。</param>
    /// <param name="cutoffMs">この時刻より前（含まない）を消す。</param>
    /// <exception cref="ArgumentNullException"><paramref name="sensorIds"/> が null。</exception>
    void DeleteBefore(TimeSeriesResolution resolution, IReadOnlyCollection<uint> sensorIds, long cutoffMs);

    /// <summary>
    /// 集計が済んでいる時刻（この時刻より前の区間は集計済み）を取得する。
    /// </summary>
    /// <param name="resolution">集計の解像度。</param>
    /// <returns>集計済みの時刻。まだ一度も集計していなければ null。</returns>
    long? GetRollupWatermark(TimeSeriesResolution resolution);

    /// <summary>
    /// 集計が済んでいる時刻を記録する。
    /// </summary>
    /// <param name="resolution">集計の解像度。</param>
    /// <param name="watermarkMs">集計済みの時刻。</param>
    void SetRollupWatermark(TimeSeriesResolution resolution, long watermarkMs);
}
