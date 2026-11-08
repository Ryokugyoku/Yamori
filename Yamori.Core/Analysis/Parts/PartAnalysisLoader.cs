using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 部品 1 つの、範囲の中の負荷・温度・クロックの記録を読み、集計値を求める。
/// </summary>
public sealed class PartAnalysisLoader
{
    private readonly ISensorService _sensors;
    private readonly AnalysisHistoryReader _reader;

    /// <summary>
    /// センサーの窓口を指定して作る。
    /// </summary>
    /// <param name="sensors">センサーの窓口。</param>
    /// <exception cref="ArgumentNullException"><paramref name="sensors"/> が null。</exception>
    public PartAnalysisLoader(ISensorService sensors)
    {
        ArgumentNullException.ThrowIfNull(sensors);
        _sensors = sensors;
        _reader = new AnalysisHistoryReader(sensors);
    }

    /// <summary>
    /// 記録を読み、集計値を求める。値の種類ごとに並行して読む。
    /// </summary>
    /// <param name="hardware">部品。</param>
    /// <param name="range">範囲。</param>
    /// <param name="catalogKeys">カタログにあるセンサーのキー。</param>
    /// <param name="manualThresholdCelsius">サーマルスロットリングの手動の注意の温度（℃）。自動なら null。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>記録と集計値。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hardware"/> か <paramref name="catalogKeys"/> が null。</exception>
    public async Task<PartAnalysis> LoadAsync(
        AnalysisHardware hardware,
        AnalysisRange range,
        IReadOnlyCollection<SensorKey> catalogKeys,
        double? manualThresholdCelsius,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hardware);
        ArgumentNullException.ThrowIfNull(catalogKeys);

        var definitions = AnalysisSeriesDefinitions.Resolve(hardware, catalogKeys);
        double? threshold = definitions.Any(d => d.Kind == AnalysisSeriesKind.Temperature)
            ? AnalysisTemperatureThreshold.Resolve(hardware, await ReadLimitAsync(hardware, cancellationToken).ConfigureAwait(false), manualThresholdCelsius)
            : null;

        var series = await Task.WhenAll(definitions.Select(async d =>
        {
            var pieces = await _reader.ReadAsync(d.Key, range, cancellationToken).ConfigureAwait(false);
            var statistics = SeriesStatisticsCalculator.Calculate(pieces, d.Kind == AnalysisSeriesKind.Temperature ? threshold : null);
            return new AnalysisSeries(d.Kind, d.Key, pieces, statistics);
        })).ConfigureAwait(false);

        return new PartAnalysis(hardware, range, series, threshold);
    }

    private async Task<double?> ReadLimitAsync(AnalysisHardware hardware, CancellationToken cancellationToken)
    {
        var keys = AnalysisTemperatureThreshold.LimitKeys(hardware);
        if (keys.Count == 0)
        {
            return null;
        }

        var readings = await _sensors.GetLatestAsync(keys.ToArray(), cancellationToken).ConfigureAwait(false);
        return keys
            .Select(k => readings.FirstOrDefault(r => r.Key == k)?.Value)
            .FirstOrDefault(v => v is { } value && double.IsFinite(value) && value > 0);
    }
}
