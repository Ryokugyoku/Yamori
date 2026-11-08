using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// グラフに重ねる 1 本の値の記録。
/// </summary>
/// <param name="Kind">値の種類。</param>
/// <param name="Key">センサーのキー。</param>
/// <param name="Pieces">記録のまとまり（古い順。1 秒ごとの記録の前を 1 分集計で補ったときは 2 つ）。</param>
/// <param name="Statistics">範囲の集計値。記録がなければ null。</param>
public sealed record AnalysisSeries(
    AnalysisSeriesKind Kind,
    SensorKey Key,
    IReadOnlyList<HistoryPiece> Pieces,
    SeriesStatistics? Statistics)
{
    /// <summary>記録があるかどうかを取得する。</summary>
    public bool HasData => Pieces.Any(p => p.Points.Count > 0);

    /// <summary>すべての点（古い順）を取得する。</summary>
    public IReadOnlyList<SensorHistoryPoint> Points => Pieces.Count == 1 ? Pieces[0].Points : Pieces.SelectMany(p => p.Points).ToArray();

    /// <summary>使った解像度を取得する（細かい順）。</summary>
    public IReadOnlyList<TimeSeriesResolution> Resolutions =>
        Pieces.Select(p => p.Resolution).Distinct().OrderBy(r => AnalysisResolutions.StepOf(r)).ToArray();

    /// <summary>いちばん粗い点が表す時間の長さを取得する（線を切る間隔の目安に使う）。記録がなければ 1 秒。</summary>
    public TimeSpan CoarsestStep => Pieces.Count == 0 ? TimeSpan.FromSeconds(1) : Pieces.Max(p => p.Step);
}
