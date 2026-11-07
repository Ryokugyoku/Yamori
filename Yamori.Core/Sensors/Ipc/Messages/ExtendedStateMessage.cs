using Yamori.Core.Sensors.Extended;

namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// 拡張センサーの状態（取得の返事と、変わったときの通知）。
/// </summary>
/// <param name="State">拡張センサーの状態。</param>
public sealed record ExtendedStateMessage(ExtendedSensorState State);
