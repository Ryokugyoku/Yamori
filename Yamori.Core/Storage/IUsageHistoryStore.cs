using Yamori.Core.Hardware;
using Yamori.Core.Monitoring;

namespace Yamori.Core.Storage;

/// <summary>
/// 使用率の履歴を PC の中に保存し、読み出す。
/// </summary>
public interface IUsageHistoryStore
{
    /// <summary>
    /// サンプルを履歴に追加する。
    /// </summary>
    /// <param name="samples">追加するサンプル。</param>
    /// <exception cref="ArgumentNullException"><paramref name="samples"/> が null。</exception>
    /// <exception cref="IOException">保存先への書き込みに失敗した。</exception>
    void Append(IReadOnlyCollection<ComponentUsageSample> samples);

    /// <summary>
    /// 指定した部品・期間の履歴を読み出す。
    /// </summary>
    /// <param name="component">部品の種類。</param>
    /// <param name="from">期間の始まり（この時刻を含む）。</param>
    /// <param name="to">期間の終わり（この時刻を含む）。</param>
    /// <returns>時刻順に並んだサンプル。</returns>
    /// <exception cref="IOException">保存先からの読み込みに失敗した。</exception>
    IReadOnlyList<ComponentUsageSample> Read(HardwareComponent component, DateTimeOffset from, DateTimeOffset to);
}
