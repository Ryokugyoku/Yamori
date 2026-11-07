namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// センサー 1 つを時系列に記録するかどうかと、その間隔・デッドバンド。マスタの <c>sensor</c> に保存する。
/// </summary>
/// <param name="Enabled">記録するかどうか。</param>
/// <param name="Interval">記録の最短の間隔。</param>
/// <param name="Deadband">前回記録した値との差がこれ未満なら記録しない（基準単位の絶対値）。0 なら常に記録する。</param>
public sealed record SensorLogPolicy(bool Enabled, TimeSpan Interval, double Deadband)
{
    /// <summary>記録しない設定を取得する。</summary>
    public static SensorLogPolicy Disabled { get; } = new(false, TimeSpan.FromSeconds(10), 0);
}
