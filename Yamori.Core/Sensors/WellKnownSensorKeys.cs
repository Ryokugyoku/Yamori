namespace Yamori.Core.Sensors;

/// <summary>
/// 画面やほかの処理から名前で参照する、よく使うセンサーのキー。
/// </summary>
public static class WellKnownSensorKeys
{
    /// <summary>CPU 全体の使用率（タスク マネージャーと同じ <c>% Processor Utility</c>）。<c>cpu/0/load/total</c>。</summary>
    public static readonly SensorKey CpuTotalLoad =
        SensorKey.Create(HardwareKind.Cpu, HardwareIds.FromSocket(0), SensorType.Load, "total");

    /// <summary>物理メモリの使用率。<c>memory/0/load/physical</c>。</summary>
    public static readonly SensorKey MemoryPhysicalLoad =
        SensorKey.Create(HardwareKind.Memory, HardwareIds.Singleton, SensorType.Load, "physical");
}
