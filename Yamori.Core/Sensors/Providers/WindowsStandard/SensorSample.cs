namespace Yamori.Core.Sensors.Providers.WindowsStandard;

/// <summary>
/// 測定結果から取り出した、センサー 1 つの説明と値の組。
/// </summary>
/// <param name="Descriptor">センサーの説明。</param>
/// <param name="Value">値（基準単位）。測れなかった場合は null。</param>
public sealed record SensorSample(SensorDescriptor Descriptor, double? Value)
{
    /// <summary>センサーのキーを取得する。</summary>
    public SensorKey Key => Descriptor.Key;
}
