namespace Yamori.Core.AppUsage.Sampling;

/// <summary>
/// PC 全体とアプリごとの CPU 使用率を測る。
/// </summary>
public interface IAppCpuSampler
{
    /// <summary>
    /// 今の値を測る。値は前回の測定からの平均になる。
    /// </summary>
    /// <param name="nowUtc">測った時刻として記録する時刻。</param>
    /// <returns>測った値。初めての測定などで、まだ値を計算できない場合は null。</returns>
    AppCpuSample? Sample(DateTimeOffset nowUtc);
}
