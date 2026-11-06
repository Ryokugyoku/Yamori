namespace Yamori.Core.Hardware;

/// <summary>
/// GPU（ビデオアダプター）1 枚の情報。
/// </summary>
/// <param name="Name">製品名。</param>
/// <param name="AdapterRamBytes">
/// 専用ビデオメモリ（バイト）。WMI の仕様上 4GB を上限とした値しか取れないことがあり、取得できない場合は null。
/// </param>
/// <param name="DriverVersion">ドライバーのバージョン。</param>
public sealed record GpuInfo(string Name, ulong? AdapterRamBytes, string DriverVersion);
