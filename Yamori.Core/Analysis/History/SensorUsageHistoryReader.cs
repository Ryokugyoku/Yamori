using Yamori.Core.Hardware;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Analysis.History;

/// <summary>
/// 常駐サービス（Helper）の時系列から、使用率の履歴を読む。1 分集計の平均を、その 1 分の使用率として使う。
/// </summary>
/// <remarks>
/// 1 分集計は 90 日保持する（<see cref="RetentionPolicy"/>）。長い期間を 1 回で頼むとパイプのメッセージの上限を超えるため、
/// <see cref="ChunkLength"/> ごとに分けて読む。Helper につながっていない場合は空になる。
/// </remarks>
public sealed class SensorUsageHistoryReader : IUsageHistoryReader
{
    /// <summary>1 回の問い合わせで読む期間の長さ（1 分集計で約 1 万点、約 1.3 MB）。</summary>
    public static readonly TimeSpan ChunkLength = TimeSpan.FromDays(7);

    private readonly ISensorService _sensors;

    /// <summary>
    /// センサーの窓口を指定して作る。
    /// </summary>
    /// <param name="sensors">センサーの窓口。</param>
    /// <exception cref="ArgumentNullException"><paramref name="sensors"/> が null。</exception>
    public SensorUsageHistoryReader(ISensorService sensors)
    {
        ArgumentNullException.ThrowIfNull(sensors);
        _sensors = sensors;
    }

    /// <inheritdoc />
    public async Task<UsageHistory> ReadAsync(
        HardwareComponent component,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        if (!UsageSensorKeys.TryGet(component, out var key) || to <= from)
        {
            return UsageHistory.Empty;
        }

        var samples = new List<UsageSample>();
        for (var chunkStart = from; chunkStart < to; chunkStart += ChunkLength)
        {
            var chunkEnd = chunkStart + ChunkLength < to ? chunkStart + ChunkLength : to;
            var points = await _sensors
                .GetHistoryAsync(key, chunkStart, chunkEnd, TimeSeriesResolution.OneMinute, cancellationToken)
                .ConfigureAwait(false);
            samples.AddRange(points
                .Where(p => p.Count > 0 && double.IsFinite(p.Average))
                .Select(p => new UsageSample(p.TimestampUtc, Math.Clamp(p.Average, 0, 100))));
        }

        return samples.Count == 0
            ? UsageHistory.Empty
            : new UsageHistory(UsageHistoryOrigin.HelperTimeSeries, samples.OrderBy(s => s.Timestamp).ToArray());
    }
}
