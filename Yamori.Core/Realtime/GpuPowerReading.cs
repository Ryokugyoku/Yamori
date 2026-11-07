namespace Yamori.Core.Realtime;

/// <summary>
/// GPU 1 つの消費電力。ドライバーは W ではなく、定格（TDP）に対する割合で報告する。
/// </summary>
/// <param name="GpuName">GPU の名前。</param>
/// <param name="PowerPercent">定格に対する割合（%）。</param>
public sealed record GpuPowerReading(string GpuName, double PowerPercent);
