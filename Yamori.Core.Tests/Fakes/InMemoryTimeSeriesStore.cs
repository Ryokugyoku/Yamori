using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// メモリの中だけに持つ時系列のストア。集計と保持期間の処理のテストに使う。
/// </summary>
internal sealed class InMemoryTimeSeriesStore : ITimeSeriesStore
{
    private readonly SortedDictionary<(uint, long), TimeSeriesPoint> _raw = [];
    private readonly Dictionary<TimeSeriesResolution, SortedDictionary<(uint, long), RollupPoint>> _rollups = new()
    {
        [TimeSeriesResolution.OneMinute] = [],
        [TimeSeriesResolution.OneHour] = [],
    };
    private readonly Dictionary<TimeSeriesResolution, long> _watermarks = [];

    public List<(TimeSeriesResolution Resolution, long Cutoff, uint[] SensorIds)> Deletes { get; } = [];

    public bool ThrowOnWrite { get; set; }

    public void WriteRaw(IReadOnlyCollection<TimeSeriesPoint> points)
    {
        if (ThrowOnWrite)
        {
            throw new IOException("disk full");
        }

        foreach (var p in points)
        {
            _raw[(p.SensorId, p.TimestampMs)] = p;
        }
    }

    public void WriteRollups(TimeSeriesResolution resolution, IReadOnlyCollection<RollupPoint> points)
    {
        foreach (var p in points)
        {
            _rollups[resolution][(p.SensorId, p.BucketStartMs)] = p;
        }
    }

    public IReadOnlyList<TimeSeriesPoint> ReadRaw(uint sensorId, long fromMs, long toMsExclusive) =>
        _raw.Values.Where(p => p.SensorId == sensorId && p.TimestampMs >= fromMs && p.TimestampMs < toMsExclusive).ToArray();

    public IReadOnlyList<RollupPoint> ReadRollups(TimeSeriesResolution resolution, uint sensorId, long fromMs, long toMsExclusive) =>
        _rollups[resolution].Values.Where(p => p.SensorId == sensorId && p.BucketStartMs >= fromMs && p.BucketStartMs < toMsExclusive).ToArray();

    public void DeleteBefore(TimeSeriesResolution resolution, IReadOnlyCollection<uint> sensorIds, long cutoffMs)
    {
        Deletes.Add((resolution, cutoffMs, sensorIds.ToArray()));
        if (resolution == TimeSeriesResolution.Raw)
        {
            foreach (var key in _raw.Keys.Where(k => sensorIds.Contains(k.Item1) && k.Item2 < cutoffMs).ToArray())
            {
                _raw.Remove(key);
            }
        }
        else
        {
            foreach (var key in _rollups[resolution].Keys.Where(k => sensorIds.Contains(k.Item1) && k.Item2 < cutoffMs).ToArray())
            {
                _rollups[resolution].Remove(key);
            }
        }
    }

    public long? GetRollupWatermark(TimeSeriesResolution resolution) =>
        _watermarks.TryGetValue(resolution, out var value) ? value : null;

    public void SetRollupWatermark(TimeSeriesResolution resolution, long watermarkMs) => _watermarks[resolution] = watermarkMs;
}
