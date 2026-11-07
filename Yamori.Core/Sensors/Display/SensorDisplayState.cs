namespace Yamori.Core.Sensors.Display;

/// <summary>
/// センサー 1 つの表示の状態。
/// </summary>
public enum SensorDisplayState
{
    /// <summary>表示しない（カタログにない、または読めない状態が続いている）。</summary>
    Hidden,

    /// <summary>値を表示する。</summary>
    Value,

    /// <summary>枠は残したまま、値の代わりに「—」を表示する（一時的に読めない）。</summary>
    Unavailable,
}
