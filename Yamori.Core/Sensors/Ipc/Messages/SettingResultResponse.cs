using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// 設定の変更の返事。
/// </summary>
/// <param name="Result">変更の結果。</param>
public sealed record SettingResultResponse(SensorSettingResult Result);
