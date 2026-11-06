using Yamori.Core.Hardware;
using Yamori.Core.Monitoring;
using Yamori.Core.Storage;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// メモリ上に履歴を持つ、テスト用の保存先。
/// </summary>
internal sealed class InMemoryUsageHistoryStore : IUsageHistoryStore
{
    /// <summary>
    /// 追加されたサンプルを取得する。
    /// </summary>
    public List<ComponentUsageSample> Samples { get; } = [];

    /// <inheritdoc />
    public void Append(IReadOnlyCollection<ComponentUsageSample> samples) => Samples.AddRange(samples);

    /// <inheritdoc />
    public IReadOnlyList<ComponentUsageSample> Read(HardwareComponent component, DateTimeOffset from, DateTimeOffset to) =>
        Samples.Where(s => s.Component == component && s.Timestamp >= from && s.Timestamp <= to).ToArray();
}
