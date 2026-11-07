namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// バッテリーの今の充放電の速さ・電圧・残りの容量を読み取る。
/// </summary>
public interface IBatteryRateSource
{
    /// <summary>
    /// バッテリーごとに今の値を読み取る。
    /// </summary>
    /// <returns>バッテリーごとの値。バッテリーがないか読めない場合は空。</returns>
    IReadOnlyList<BatteryRate> Read();
}
