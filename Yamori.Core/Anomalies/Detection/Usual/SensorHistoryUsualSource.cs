using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 記録の 1 分集計（平均）から、負荷と温度を分ごとに組にして読む。
/// </summary>
/// <remarks>
/// 記録はデッドバンドで省かれることがある（変化が小さい間は最長 1 分ごと）ため、温度がその分にないときは、
/// <see cref="CarryForward"/> 以内の直前の温度を使う。
/// </remarks>
public sealed class SensorHistoryUsualSource : IUsualHistorySource
{
    /// <summary>温度がその分にないとき、直前の温度を使ってよい長さ。</summary>
    public static readonly TimeSpan CarryForward = TimeSpan.FromMinutes(2);

    private readonly AnomalyHistoryReader _reader;

    /// <summary>
    /// 記録の読み方を指定して作る。
    /// </summary>
    /// <param name="reader">記録の読み方。</param>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> が null。</exception>
    public SensorHistoryUsualSource(AnomalyHistoryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _reader = reader;
    }

    /// <inheritdoc />
    public IReadOnlyList<UsualMinute> Read(UsualPartDefinition part, DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        ArgumentNullException.ThrowIfNull(part);

        var loads = _reader(part.LoadKey, fromUtc, toUtc, TimeSeriesResolution.OneMinute);
        var temperatures = part.TemperatureKey is { } key ? _reader(key, fromUtc, toUtc, TimeSeriesResolution.OneMinute) : [];

        var result = new List<UsualMinute>(loads.Count);
        var t = 0;
        foreach (var load in loads)
        {
            if (!double.IsFinite(load.Average))
            {
                continue;
            }

            // 温度の点のうち、この分より後でない最後のもの。
            while (t + 1 < temperatures.Count && temperatures[t + 1].TimestampUtc <= load.TimestampUtc)
            {
                t++;
            }

            double? temperature = null;
            if (t < temperatures.Count
                && temperatures[t].TimestampUtc <= load.TimestampUtc
                && load.TimestampUtc - temperatures[t].TimestampUtc <= CarryForward
                && double.IsFinite(temperatures[t].Average))
            {
                temperature = temperatures[t].Average;
            }

            result.Add(new UsualMinute(load.TimestampUtc, load.Average, temperature));
        }

        return result;
    }
}
