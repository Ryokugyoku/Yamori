using Yamori.Core.Realtime;

namespace Yamori.Core.Sensors.Providers.WindowsStandard;

/// <summary>
/// 標準 API の測定結果（<see cref="RealtimeSnapshot"/>）から、1 種類のハードウェアのセンサーを取り出す。
/// </summary>
public interface ISnapshotSensorMapper
{
    /// <summary>
    /// 測定結果からセンサーを取り出す。
    /// </summary>
    /// <param name="snapshot">測定結果。</param>
    /// <returns>センサーの説明と値。そのハードウェアがなければ空。測れなかった値は null のまま含める。</returns>
    IEnumerable<SensorSample> Map(RealtimeSnapshot snapshot);
}
