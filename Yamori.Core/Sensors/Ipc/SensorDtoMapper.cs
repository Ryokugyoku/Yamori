using Yamori.Core.Sensors.Ipc.Messages;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Sensors.Ipc;

/// <summary>
/// センサーの型と、パイプで送る形（<see cref="Messages"/>）の間の変換。
/// </summary>
public static class SensorDtoMapper
{
    /// <summary>説明を送る形にする。</summary>
    /// <param name="descriptor">説明。</param>
    /// <returns>送る形。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="descriptor"/> が null。</exception>
    public static SensorDescriptorDto ToDto(SensorDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return new(descriptor.Key.ToString(), descriptor.Unit, descriptor.DisplayName, descriptor.HardwareName, descriptor.Group, descriptor.ProviderId);
    }

    /// <summary>送られてきた説明を戻す。</summary>
    /// <param name="dto">送られてきた説明。</param>
    /// <returns>説明。キーが読めない場合は null（新しい版の相手が知らない種類を送ってきた場合など）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dto"/> が null。</exception>
    public static SensorDescriptor? FromDto(SensorDescriptorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return SensorKey.TryParse(dto.Key, out var key)
            ? new SensorDescriptor(key, dto.Unit, dto.DisplayName ?? string.Empty, dto.HardwareName ?? string.Empty, dto.Group ?? string.Empty, dto.ProviderId ?? string.Empty)
            : null;
    }

    /// <summary>値を送る形にする。</summary>
    /// <param name="reading">値。</param>
    /// <returns>送る形。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reading"/> が null。</exception>
    public static SensorReadingDto ToDto(SensorReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return new(reading.Key.ToString(), reading.Value, reading.TimestampUtc, reading.SourceProviderId, reading.Status);
    }

    /// <summary>送られてきた値を戻す。</summary>
    /// <param name="dto">送られてきた値。</param>
    /// <returns>値。キーが読めない場合は null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dto"/> が null。</exception>
    public static SensorReading? FromDto(SensorReadingDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (!SensorKey.TryParse(dto.Key, out var key))
        {
            return null;
        }

        // 値のある失敗、値のない Ok のような矛盾は、値のほうを状態に合わせる。
        var value = dto.Status == SensorStatus.Ok ? dto.Value : null;
        var status = dto.Status == SensorStatus.Ok && value is null ? SensorStatus.Error : dto.Status;
        return new SensorReading(key, value, dto.TimestampUtc.ToUniversalTime(), dto.SourceProviderId ?? string.Empty, status);
    }

    /// <summary>履歴の点を送る形にする。</summary>
    /// <param name="point">履歴の点。</param>
    /// <returns>送る形。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="point"/> が null。</exception>
    public static SensorHistoryPointDto ToDto(SensorHistoryPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);
        return new(point.TimestampUtc, point.Min, point.Max, point.Average, point.Count);
    }

    /// <summary>送られてきた履歴の点を戻す。</summary>
    /// <param name="dto">送られてきた点。</param>
    /// <returns>履歴の点。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dto"/> が null。</exception>
    public static SensorHistoryPoint FromDto(SensorHistoryPointDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new(dto.TimestampUtc.ToUniversalTime(), dto.Min, dto.Max, dto.Average, dto.Count);
    }

    /// <summary>記録の設定を送る形にする。</summary>
    /// <param name="policy">記録の設定。</param>
    /// <returns>送る形。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="policy"/> が null。</exception>
    public static SensorLogPolicyDto ToDto(SensorLogPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return new(policy.Enabled, (long)policy.Interval.TotalMilliseconds, policy.Deadband);
    }

    /// <summary>送られてきた記録の設定を戻す。</summary>
    /// <param name="dto">送られてきた設定。</param>
    /// <returns>記録の設定。間隔とデッドバンドが正しくない場合は null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dto"/> が null。</exception>
    public static SensorLogPolicy? FromDto(SensorLogPolicyDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return dto.IntervalMilliseconds > 0 && double.IsFinite(dto.Deadband) && dto.Deadband >= 0
            ? new SensorLogPolicy(dto.Enabled, TimeSpan.FromMilliseconds(dto.IntervalMilliseconds), dto.Deadband)
            : null;
    }
}
