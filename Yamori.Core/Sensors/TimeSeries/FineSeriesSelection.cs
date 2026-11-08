namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 1 秒ごとの記録（<see cref="TimeSeriesResolution.OneSecond"/>）の対象を選ぶ。異常の検出と分析に使う一部のセンサー（約 20〜40 個）だけを、
/// ユーザーの記録の設定とは別に、毎秒・デッドバンドなしで残す。
/// </summary>
/// <remarks>
/// 全センサーを毎秒残すと 1 日に数百 MB になるため、負荷・温度・クロック・電力・性能の制限など、
/// 数秒の現象（スロットリングなど）を見るのに要るものに絞る（数十 MB に収める）。
/// </remarks>
public sealed class FineSeriesSelection
{
    /// <summary>1 秒ごとの記録に使う記録の設定（毎秒の点をすべて残す。収集の間隔の揺れで落とさないよう、間隔は 1 秒より短くする）。</summary>
    public static readonly SensorLogPolicy Policy = new(true, TimeSpan.FromMilliseconds(500), 0);

    /// <summary>既定の対象のパターン。</summary>
    public static IReadOnlyList<string> DefaultPatterns { get; } =
    [
        "cpu/*/load/total",
        "cpu/*/temperature/package",
        "cpu/*/clock/average-effective",
        "cpu/*/clock/average-estimated",
        "cpu/*/power/package",
        "cpu/*/power/cores",
        "cpu/*/level/performance",
        "cpu/*/level/performance-limit",
        "cpu/*/flag/*",
        "gpu/*/load/max-engine",
        "gpu/*/load/power-tdp",
        "gpu/*/temperature/adapter",
        "gpu/*/temperature/core",
        "gpu/*/clock/core",
        "gpu/*/fan/adapter",
        "memory/*/load/physical",
        "storage/*/temperature/drive",
        "thermal-zone/*/temperature/zone",
        "thermal-zone/*/level/passive-limit",
        "thermal-zone/*/flag/*",
    ];

    private readonly IReadOnlyList<SensorKeyPattern> _patterns;

    /// <summary>
    /// 対象のパターンを指定して作る。
    /// </summary>
    /// <param name="patterns">対象のパターン（<see cref="SensorKeyPattern"/> の表記）。省略時は <see cref="DefaultPatterns"/>。</param>
    /// <exception cref="FormatException">パターンの表記が正しくない。</exception>
    public FineSeriesSelection(IEnumerable<string>? patterns = null)
    {
        _patterns = (patterns ?? DefaultPatterns).Select(SensorKeyPattern.Parse).ToArray();
    }

    /// <summary>
    /// センサーが 1 秒ごとの記録の対象かどうかを調べる。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <returns>対象なら true。</returns>
    public bool Includes(SensorKey key) => _patterns.Any(p => p.Matches(key));

    /// <summary>
    /// 記録の対象（通常の記録の対象と同じ番号を使う）から、1 秒ごとの記録の対象を選ぶ。記録の設定は <see cref="Policy"/> に置き換える。
    /// </summary>
    /// <param name="targets">キーごとの通常の記録の対象（無効にしたセンサーも含めたマスタのすべてのセンサー）。</param>
    /// <returns>キーごとの 1 秒ごとの記録の対象。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="targets"/> が null。</exception>
    public IReadOnlyDictionary<SensorKey, SensorLogTarget> Select(IReadOnlyDictionary<SensorKey, SensorLogTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        return targets
            .Where(t => Includes(t.Key))
            .ToDictionary(t => t.Key, t => t.Value with { Policy = Policy });
    }
}
