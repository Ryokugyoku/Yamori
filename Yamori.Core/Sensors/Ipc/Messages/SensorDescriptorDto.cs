namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// パイプで送る <see cref="SensorDescriptor"/>。項目は同じで、キーは文字列表記にする。
/// </summary>
/// <param name="Key">センサーのキーの文字列表記。</param>
/// <param name="Unit">値の単位。</param>
/// <param name="DisplayName">センサーの表示名。</param>
/// <param name="HardwareName">所属するハードウェアの表示名。</param>
/// <param name="Group">ハードウェアの中のグループ。</param>
/// <param name="ProviderId">宣言した Provider の ID。</param>
public sealed record SensorDescriptorDto(string Key, SensorUnit Unit, string DisplayName, string HardwareName, string Group, string ProviderId);
