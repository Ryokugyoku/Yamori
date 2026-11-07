namespace Yamori.Core.Hardware;

/// <summary>
/// 電源まわりの情報。電源ユニット、バッテリー、今の電源の種類をまとめる。
/// </summary>
/// <param name="Units">ファームウェア（SMBIOS）が報告している電源ユニットの一覧。報告しない PC が多い。</param>
/// <param name="Batteries">バッテリーの一覧。デスクトップ PC では空。</param>
/// <param name="Source">今使っている電源。</param>
public sealed record PowerSupplyInfo(
    IReadOnlyList<PowerSupplyUnitInfo> Units,
    IReadOnlyList<BatteryInfo> Batteries,
    PowerSource Source)
{
    /// <summary>
    /// 何も取得できなかったことを表す空の情報。
    /// </summary>
    public static PowerSupplyInfo Empty { get; } = new([], [], PowerSource.Unknown);
}
