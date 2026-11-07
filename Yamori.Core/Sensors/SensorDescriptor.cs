namespace Yamori.Core.Sensors;

/// <summary>
/// Provider が「この環境で提供できる」と宣言したセンサー 1 つの説明。IPC でもこの形のまま送る。
/// </summary>
/// <param name="Key">センサーのキー。</param>
/// <param name="Unit">値の単位（基準単位に正規化済み）。</param>
/// <param name="DisplayName">センサーの表示名（取得元が付けた名前。UI の言語に合わせた名前は UI 側で決める）。</param>
/// <param name="HardwareName">所属するハードウェアの表示名（ツリーの 1 段目。例：<c>NVIDIA GeForce RTX 4070</c>）。</param>
/// <param name="Group">ハードウェアの中のグループ（ツリーの 2 段目。例：<c>load</c>、<c>threads</c>）。</param>
/// <param name="ProviderId">宣言した Provider の ID。</param>
public sealed record SensorDescriptor(
    SensorKey Key,
    SensorUnit Unit,
    string DisplayName,
    string HardwareName,
    string Group,
    string ProviderId)
{
    /// <summary>ハードウェアの種類を取得する。</summary>
    public HardwareKind HardwareKind => Key.HardwareKind;

    /// <summary>量の種類を取得する。</summary>
    public SensorType SensorType => Key.SensorType;
}
