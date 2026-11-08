namespace Yamori.Core.Anomalies.Detection.Throttling;

/// <summary>
/// 部品の種類 1 つ（CPU・GPU・温度ゾーン）について、今の値からスロットリングの判定の材料を読む。
/// </summary>
public interface IThrottlingSignalSource
{
    /// <summary>
    /// 今の値から、部品ごとの判定の材料を読む。
    /// </summary>
    /// <param name="frame">判定の材料（今の値）。</param>
    /// <returns>判定できる値のある部品ごとの材料。</returns>
    IEnumerable<ThrottlingObservation> Read(AnomalyFrame frame);
}
