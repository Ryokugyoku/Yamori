using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 同じ解像度で読んだ、ひと続きの記録。
/// </summary>
/// <param name="Resolution">解像度。</param>
/// <param name="Points">点（古い順）。</param>
public sealed record HistoryPiece(TimeSeriesResolution Resolution, IReadOnlyList<SensorHistoryPoint> Points)
{
    /// <summary>1 点が表す時間の長さを取得する。</summary>
    public TimeSpan Step => AnalysisResolutions.StepOf(Resolution);
}
