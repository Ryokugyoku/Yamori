using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// メモリの中だけに持つ時系列のストア。集計と保持期間の処理のテストに使う。
/// </summary>
internal sealed class InMemoryTimeSeriesStore : ITimeSeriesStore
{
    private readonly Dictionary<TimeSeriesResolution, SortedDictionary<(uint, long), TimeSeriesPoint>> _points = new()
    {
        [TimeSeriesResolution.Raw] = [],
        [TimeSeriesResolution.OneSecond] = [],
    };
    private readonly Dictionary<TimeSeriesResolution, SortedDictionary<(uint, long), RollupPoint>> _rollups = new()
    {
        [TimeSeriesResolution.OneMinute] = [],
        [TimeSeriesResolution.OneHour] = [],
    };
    private readonly Dictionary<TimeSeriesResolution, long> _watermarks = [];

    public List<(TimeSeriesResolution Resolution, long Cutoff, uint[] SensorIds)> Deletes { get; } = [];

    public bool ThrowOnWrite { get; set; }

    public void WritePoints(TimeSeriesResolution resolution, IReadOnlyCollection<TimeSeriesPoint> points)
    {
        if (ThrowOnWrite)
        {
            throw new IOException("disk full");
        }

        foreach (var p in points)
        {
            _points[resolution][(p.SensorId, p.TimestampMs)] = p;
        }
    }

    public void WriteRaw(IReadOnlyCollection<TimeSeriesPoint> points) => WritePoints(TimeSeriesResolution.Raw, points);

    public void WriteRollups(TimeSeriesResolution resolution, IReadOnlyCollection<RollupPoint> points)
    {
        foreach (var p in points)
        {
            _rollups[resolution][(p.SensorId, p.BucketStartMs)] = p;
        }
    }

    public IReadOnlyList<TimeSeriesPoint> ReadPoints(TimeSeriesResolution resolution, uint sensorId, long fromMs, long toMsExclusive) =>
        _points[resolution].Values.Where(p => p.SensorId == sensorId && p.TimestampMs >= fromMs && p.TimestampMs < toMsExclusive).ToArray();

    public IReadOnlyList<TimeSeriesPoint> ReadRaw(uint sensorId, long fromMs, long toMsExclusive) =>
        ReadPoints(TimeSeriesResolution.Raw, sensorId, fromMs, toMsExclusive);

    public IReadOnlyList<RollupPoint> ReadRollups(TimeSeriesResolution resolution, uint sensorId, long fromMs, long toMsExclusive) =>
        _rollups[resolution].Values.Where(p => p.SensorId == sensorId && p.BucketStartMs >= fromMs && p.BucketStartMs < toMsExclusive).ToArray();

    public void DeleteBefore(TimeSeriesResolution resolution, IReadOnlyCollection<uint> sensorIds, long cutoffMs)
    {
        Deletes.Add((resolution, cutoffMs, sensorIds.ToArray()));
        if (_points.TryGetValue(resolution, out var points))
        {
            foreach (var key in points.Keys.Where(k => sensorIds.Contains(k.Item1) && k.Item2 < cutoffMs).ToArray())
            {
                points.Remove(key);
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
