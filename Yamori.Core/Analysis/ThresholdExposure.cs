namespace Yamori.Core.Analysis;

/// <summary>
/// しきい値を超えていた時間の割合。
/// </summary>
/// <param name="ThresholdPercent">しきい値（例：80）。</param>
/// <param name="TimeAbove">しきい値を超えていた時間。</param>
/// <param name="TotalObserved">測っていた時間の合計。</param>
public readonly record struct ThresholdExposure(double ThresholdPercent, TimeSpan TimeAbove, TimeSpan TotalObserved)
{
    /// <summary>
    /// しきい値を超えていた時間の割合（0〜1）を取得する。
    /// </summary>
    /// <returns>測った時間がなければ 0。</returns>
    public double Ratio => TotalObserved > TimeSpan.Zero ? TimeAbove / TotalObserved : 0;
}
