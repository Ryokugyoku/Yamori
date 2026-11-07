namespace Yamori.Core.Hardware.Smbios;

/// <summary>
/// SMBIOS の状態の値（3 ビット）を <see cref="DeviceStatus"/> にする。
/// </summary>
public static class SmbiosStatus
{
    /// <summary>
    /// 状態の値を変換する。
    /// </summary>
    /// <param name="value">SMBIOS の状態の値（1〜6）。</param>
    /// <returns>状態。範囲外の値は <see cref="DeviceStatus.Unknown"/>。</returns>
    public static DeviceStatus FromValue(int value) =>
        Enum.IsDefined(typeof(DeviceStatus), value) ? (DeviceStatus)value : DeviceStatus.Unknown;
}
