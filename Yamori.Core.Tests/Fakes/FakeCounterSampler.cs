using Yamori.Core.Realtime.Counters;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// 決めた値を返すパフォーマンスカウンターの読み出し元。
/// </summary>
internal sealed class FakeCounterSampler : IPerformanceCounterSampler
{
    private readonly Dictionary<string, IReadOnlyDictionary<string, double>> _values = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>取り込みが呼ばれた回数を取得する。</summary>
    public int CollectCount { get; private set; }

    /// <summary>
    /// カウンターの値をインスタンスごとに決める。値を決めたカウンターは「この PC にある」ものとして扱う。
    /// </summary>
    /// <param name="path">カウンターのパス。</param>
    /// <param name="values">インスタンス名と値の組。</param>
    /// <returns>このインスタンス。</returns>
    public FakeCounterSampler Set(string path, params (string Instance, double Value)[] values)
    {
        _values[path] = values.ToDictionary(v => v.Instance, v => v.Value);
        return this;
    }

    /// <summary>
    /// インスタンスのないカウンターの値を決める。
    /// </summary>
    /// <param name="path">カウンターのパス。</param>
    /// <param name="value">値。</param>
    /// <returns>このインスタンス。</returns>
    public FakeCounterSampler SetSingle(string path, double value) => Set(path, (string.Empty, value));

    /// <inheritdoc />
    public void Collect() => CollectCount++;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, double> Read(string counterPath) =>
        _values.TryGetValue(counterPath, out var values) ? values : new Dictionary<string, double>();

    /// <inheritdoc />
    public bool IsAvailable(string counterPath) => _values.ContainsKey(counterPath);

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
