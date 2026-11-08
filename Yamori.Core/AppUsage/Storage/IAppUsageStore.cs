namespace Yamori.Core.AppUsage.Storage;

/// <summary>
/// アプリごとの CPU 使用率の 1 分ごとの記録を保存する。実装はスレッドセーフであること。
/// </summary>
public interface IAppUsageStore
{
    /// <summary>
    /// 1 分の記録を書き込む。同じ 1 分の記録がすでにあれば、測定の回数で重みをつけて合わせる（Helper を途中で再起動した場合など）。
    /// </summary>
    /// <param name="minute">1 分の記録。</param>
    /// <exception cref="ArgumentNullException"><paramref name="minute"/> が null。</exception>
    void WriteMinute(AppCpuMinute minute);

    /// <summary>
    /// 指定した時刻より前の記録と、どの記録からも使われなくなったアプリの名前を消す。
    /// </summary>
    /// <param name="cutoffUtc">この時刻より前（含まない）の 1 分を消す。</param>
    void DeleteBefore(DateTimeOffset cutoffUtc);

    /// <summary>
    /// 期間の中の記録を、アプリごとに合計する。
    /// </summary>
    /// <param name="fromUtc">期間の始まり（含む）。</param>
    /// <param name="toUtc">期間の終わり（含まない）。</param>
    /// <param name="highLoadThresholdPercent">PC 全体の使用率がこれを超えていた 1 分を「高負荷」として別に合計する（%）。</param>
    /// <returns>合計。</returns>
    AppCpuTotals ReadTotals(DateTimeOffset fromUtc, DateTimeOffset toUtc, double highLoadThresholdPercent);
}
