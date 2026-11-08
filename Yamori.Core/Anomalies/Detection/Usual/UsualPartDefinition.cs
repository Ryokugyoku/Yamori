using Yamori.Core.Sensors;

namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 「ふだんより悪い」を判定する部品 1 つと、使うセンサー。
/// </summary>
/// <param name="HardwareKey">部品のキー（<c>{hardwareKind}/{hardwareId}</c>）。</param>
/// <param name="HardwareKind">部品の種類。</param>
/// <param name="LoadKey">負荷のセンサー（待機中かどうかと、待機中の負荷に使う）。</param>
/// <param name="TemperatureKey">温度のセンサー。なければ温度は比べない。</param>
/// <param name="CompareIdleLoad">待機中の負荷も比べるなら true（裏で動くアプリの兆し。CPU だけ）。</param>
public sealed record UsualPartDefinition(
    string HardwareKey,
    HardwareKind HardwareKind,
    SensorKey LoadKey,
    SensorKey? TemperatureKey,
    bool CompareIdleLoad);
