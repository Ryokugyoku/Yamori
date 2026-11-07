namespace Yamori.Core.Realtime;

/// <summary>
/// 電力量計（CPU の RAPL や、ファームウェアの電力計）1 つの今の値。
/// </summary>
/// <param name="Name">Windows が付けた電力量計の名前（例：<c>rapl_package0_pkg</c>）。</param>
/// <param name="Domain">測っている範囲。</param>
/// <param name="Watts">今の消費電力（W）。</param>
public sealed record PowerMeterReading(string Name, PowerMeterDomain Domain, double Watts);
