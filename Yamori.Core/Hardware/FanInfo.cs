namespace Yamori.Core.Hardware;

/// <summary>
/// ファームウェア（SMBIOS）が報告している冷却装置 1 つの情報。
/// </summary>
/// <remarks>
/// 回転数の実測値は、Windows が標準では公開していない（読み取りにはカーネルドライバーが必要）ため含まない。
/// </remarks>
/// <param name="Description">ファームウェアが付けた名前。設定されていない場合は null。</param>
/// <param name="Type">冷却装置の種類。</param>
/// <param name="Status">ファームウェアが報告する状態。</param>
/// <param name="NominalSpeedRpm">公称の回転数（rpm）。ファームウェアが報告していない場合は null。</param>
public sealed record FanInfo(string? Description, CoolingDeviceType Type, DeviceStatus Status, int? NominalSpeedRpm);
