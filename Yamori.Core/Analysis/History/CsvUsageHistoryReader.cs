using Yamori.Core.Hardware;
using Yamori.Core.Storage;

namespace Yamori.Core.Analysis.History;

/// <summary>
/// 画面のプロセスが記録した使用率の履歴（<see cref="IUsageHistoryStore"/>。CSV）を読む。
/// </summary>
public sealed class CsvUsageHistoryReader : IUsageHistoryReader
{
    private readonly IUsageHistoryStore _store;

    /// <summary>
    /// 履歴の保存先を指定して作る。
    /// </summary>
    /// <param name="store">使用率の履歴の保存先。</param>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> が null。</exception>
    public CsvUsageHistoryReader(IUsageHistoryStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc />
    public Task<UsageHistory> ReadAsync(
        HardwareComponent component,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var samples = _store.Read(component, from, to).Select(s => s.ToUsageSample()).ToArray();
        return Task.FromResult(samples.Length == 0 ? UsageHistory.Empty : new UsageHistory(UsageHistoryOrigin.AppCsv, samples));
    }
}
