namespace Yamori.Core.Sensors;

/// <summary>
/// センサー 1 つの、ある時点の値。IPC でもこの形のまま送る。
/// </summary>
/// <param name="Key">センサーのキー。</param>
/// <param name="Value">値（基準単位）。<see cref="Status"/> が <see cref="SensorStatus.Ok"/> 以外なら null。</param>
/// <param name="TimestampUtc">読んだ時刻（UTC）。</param>
/// <param name="SourceProviderId">値を採用した Provider の ID。どの Provider も提供していなければ空文字列。</param>
/// <param name="Status">読んだ結果の状態。</param>
public sealed record SensorReading(
    SensorKey Key,
    double? Value,
    DateTimeOffset TimestampUtc,
    string SourceProviderId,
    SensorStatus Status)
{
    /// <summary>
    /// 値を読めた結果を作る。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <param name="value">値（基準単位）。</param>
    /// <param name="timestampUtc">読んだ時刻。</param>
    /// <param name="sourceProviderId">読んだ Provider の ID。</param>
    /// <returns>状態が <see cref="SensorStatus.Ok"/> の結果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceProviderId"/> が null。</exception>
    public static SensorReading Ok(SensorKey key, double value, DateTimeOffset timestampUtc, string sourceProviderId)
    {
        ArgumentNullException.ThrowIfNull(sourceProviderId);
        return new SensorReading(key, value, timestampUtc.ToUniversalTime(), sourceProviderId, SensorStatus.Ok);
    }

    /// <summary>
    /// 値を読めなかった結果を作る。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <param name="status">状態。<see cref="SensorStatus.Ok"/> は指定できない。</param>
    /// <param name="timestampUtc">読もうとした時刻。</param>
    /// <param name="sourceProviderId">読もうとした Provider の ID。どの Provider も提供していなければ空文字列。</param>
    /// <returns>値が null の結果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceProviderId"/> が null。</exception>
    /// <exception cref="ArgumentException"><paramref name="status"/> が <see cref="SensorStatus.Ok"/>。</exception>
    public static SensorReading Failed(SensorKey key, SensorStatus status, DateTimeOffset timestampUtc, string sourceProviderId)
    {
        ArgumentNullException.ThrowIfNull(sourceProviderId);
        if (status == SensorStatus.Ok)
        {
            throw new ArgumentException("値のない結果に Ok は指定できません。", nameof(status));
        }

        return new SensorReading(key, null, timestampUtc.ToUniversalTime(), sourceProviderId, status);
    }
}
