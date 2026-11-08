using Yamori.Core.Hardware;
using Yamori.Core.Sensors;

namespace Yamori.Core.Analysis.History;

/// <summary>
/// 負荷時間の分析に使う、部品ごとの使用率のセンサーのキー。
/// </summary>
public static class UsageSensorKeys
{
    /// <summary>
    /// 部品の使用率を表すセンサーのキーを探す。
    /// </summary>
    /// <param name="component">部品の種類。</param>
    /// <param name="key">見つかったキー。</param>
    /// <returns>対応するセンサーがあれば true。</returns>
    public static bool TryGet(HardwareComponent component, out SensorKey key)
    {
        switch (component)
        {
            case HardwareComponent.Cpu:
                key = WellKnownSensorKeys.CpuTotalLoad;
                return true;
            case HardwareComponent.Memory:
                key = WellKnownSensorKeys.MemoryPhysicalLoad;
                return true;
            default:
                key = default;
                return false;
        }
    }
}
