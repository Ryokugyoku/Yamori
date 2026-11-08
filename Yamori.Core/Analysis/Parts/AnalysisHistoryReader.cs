using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 分析する範囲の記録を、範囲の長さに合った解像度（<see cref="AnalysisResolutions"/>）で読む。
/// 長い範囲はパイプの 1 通の上限を超えないよう、区切って読む。
/// </summary>
/// <remarks>
/// 1 秒ごとの記録が範囲の途中から始まるとき（入れたばかり、1 秒ごとの記録が 24 時間より短いなど）は、その前を 1 分集計で補う。
/// </remarks>
public sealed class AnalysisHistoryReader
{
    /// <summary>1 秒ごとの記録の始まりが範囲の始まりからこれ以上遅れたら、その前を 1 分集計で補う。</summary>
    public static readonly TimeSpan FillThreshold = TimeSpan.FromMinutes(2);

    private readonly ISensorService _sensors;

    /// <summary>
    /// センサーの窓口を指定して作る。
    /// </summary>
    /// <param name="sensors">センサーの窓口。</param>
    /// <exception cref="ArgumentNullException"><paramref name="sensors"/> が null。</exception>
    public AnalysisHistoryReader(ISensorService sensors)
    {
        ArgumentNullException.ThrowIfNull(sensors);
        _sensors = sensors;
    }

    /// <summary>
    /// 記録を読む。1 秒ごとの記録が空なら（対象でないセンサーなど）1 分集計で読み直し、途中から始まるならその前を 1 分集計で補う。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <param name="range">範囲。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>古い順の記録のまとまり。記録がなければ空。</returns>
    public async Task<IReadOnlyList<HistoryPiece>> ReadAsync(
        SensorKey key,
        AnalysisRange range,
        CancellationToken cancellationToken = default)
    {
        TimeSeriesResolution? resolution = AnalysisResolutions.Choose(range);
        while (resolution is { } current)
        {
            var points = await ReadChunksAsync(key, range, current, cancellationToken).ConfigureAwait(false);
            if (points.Count > 0)
            {
                var pieces = new List<HistoryPiece>();
                if (AnalysisResolutions.Fallback(current) is { } coarse && points[0].TimestampUtc - range.FromUtc >= FillThreshold)
                {
                    var before = await ReadChunksAsync(key, new AnalysisRange(range.FromUtc, points[0].TimestampUtc), coarse, cancellationToken).ConfigureAwait(false);
                    if (before.Count > 0)
                    {
                        pieces.Add(new HistoryPiece(coarse, before));
                    }
                }

                pieces.Add(new HistoryPiece(current, points));
                return pieces;
            }

            resolution = AnalysisResolutions.Fallback(current);
        }

        return [];
    }

    private async Task<IReadOnlyList<SensorHistoryPoint>> ReadChunksAsync(
        SensorKey key,
        AnalysisRange range,
        TimeSeriesResolution resolution,
        CancellationToken cancellationToken)
    {
        var chunk = AnalysisResolutions.ChunkLength(resolution);
        var points = new List<SensorHistoryPoint>();
        for (var start = range.FromUtc; start < range.ToUtc; start += chunk)
        {
            var end = start + chunk < range.ToUtc ? start + chunk : range.ToUtc;
            points.AddRange(await _sensors.GetHistoryAsync(key, start, end, resolution, cancellationToken).ConfigureAwait(false));
        }

        return points
            .Where(p => double.IsFinite(p.Average))
            .OrderBy(p => p.TimestampUtc)
            .ToArray();
    }
}
