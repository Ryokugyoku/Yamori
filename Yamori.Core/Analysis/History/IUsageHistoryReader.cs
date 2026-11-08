using Yamori.Core.Hardware;

namespace Yamori.Core.Analysis.History;

/// <summary>
/// 負荷時間の分析に使う、部品ごとの使用率の履歴を読む。
/// </summary>
public interface IUsageHistoryReader
{
    /// <summary>
    /// 指定した部品・期間の使用率の履歴を読む。
    /// </summary>
    /// <param name="component">部品の種類。</param>
    /// <param name="from">期間の始まり（含む）。</param>
    /// <param name="to">期間の終わり。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>履歴。この読み取り元に記録がなければ <see cref="UsageHistory.Empty"/>。</returns>
    /// <exception cref="IOException">保存先からの読み込みに失敗した。</exception>
    Task<UsageHistory> ReadAsync(
        HardwareComponent component,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}
