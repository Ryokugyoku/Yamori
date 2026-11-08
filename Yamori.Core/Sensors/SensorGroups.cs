namespace Yamori.Core.Sensors;

/// <summary>
/// 取得元をまたいで使うセンサーのグループ（<see cref="SensorDescriptor.Group"/>）の名前。
/// </summary>
public static class SensorGroups
{
    /// <summary>性能の制限の印（熱・電力・電流による制限、温度ゾーンの Throttle Reasons）のグループ。</summary>
    public const string Throttling = "throttling";
}
