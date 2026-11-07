namespace Yamori.Core.Hardware;

/// <summary>
/// バッテリー 1 個の情報。
/// </summary>
/// <param name="Name">名前。</param>
/// <param name="ChargePercent">現在の残量（%）。取得できない場合は null。</param>
/// <param name="DesignCapacityMwh">設計上の容量（mWh）。取得できない場合は null。</param>
/// <param name="FullChargeCapacityMwh">現在の満充電容量（mWh）。取得できない場合は null。</param>
public sealed record BatteryInfo(
    string Name,
    int? ChargePercent,
    int? DesignCapacityMwh,
    int? FullChargeCapacityMwh)
{
    /// <summary>充放電の回数。取得できない場合は null。</summary>
    public int? CycleCount { get; init; }

    /// <summary>
    /// 設計容量に対する現在の満充電容量の割合（0〜1）を取得する。劣化の目安になる。
    /// </summary>
    /// <returns>容量が取得できない場合は null。</returns>
    public double? HealthRatio =>
        DesignCapacityMwh is > 0 && FullChargeCapacityMwh is { } full
            ? (double)full / DesignCapacityMwh.Value
            : null;
}
