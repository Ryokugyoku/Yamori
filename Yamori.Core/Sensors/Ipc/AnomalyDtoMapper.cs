using Yamori.Core.Anomalies;
using Yamori.Core.Sensors.Ipc.Messages;

namespace Yamori.Core.Sensors.Ipc;

/// <summary>
/// 異常の型と、パイプで送る形（DTO）の間の変換。
/// </summary>
public static class AnomalyDtoMapper
{
    /// <summary>異常を送る形にする。</summary>
    /// <param name="anomaly">異常。</param>
    /// <returns>送る形。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="anomaly"/> が null。</exception>
    public static AnomalyDto ToDto(AnomalyRecord anomaly)
    {
        ArgumentNullException.ThrowIfNull(anomaly);
        return new AnomalyDto(
            anomaly.AnomalyId,
            anomaly.Kind,
            anomaly.HardwareKey,
            anomaly.HardwareKind,
            anomaly.HardwareName,
            anomaly.StartedUtc,
            anomaly.LastSeenUtc,
            anomaly.EndedUtc,
            anomaly.Severity,
            anomaly.Evidence.Where(e => double.IsFinite(e.Value)).ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal),
            anomaly.DismissedUtc,
            anomaly.NotifiedUtc);
    }

    /// <summary>送られてきた異常を戻す。</summary>
    /// <param name="dto">送られてきた異常。</param>
    /// <returns>異常。知らない種類・重さ、部品のキーがないなど、扱えない場合は null（新しい版の Helper が送ってきた場合など）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dto"/> が null。</exception>
    public static AnomalyRecord? FromDto(AnomalyDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (!Enum.IsDefined(dto.Kind) || !Enum.IsDefined(dto.Severity) || string.IsNullOrEmpty(dto.HardwareKey) || dto.AnomalyId <= 0)
        {
            return null;
        }

        var evidence = (dto.Evidence ?? [])
            .Where(e => e.Key is not null && double.IsFinite(e.Value))
            .ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal);
        return new AnomalyRecord(
            dto.AnomalyId,
            dto.Kind,
            dto.HardwareKey,
            dto.HardwareKind,
            dto.HardwareName ?? string.Empty,
            dto.StartedUtc.ToUniversalTime(),
            dto.LastSeenUtc.ToUniversalTime(),
            dto.EndedUtc?.ToUniversalTime(),
            dto.Severity,
            evidence,
            dto.DismissedUtc?.ToUniversalTime(),
            dto.NotifiedUtc?.ToUniversalTime());
    }

    /// <summary>知らせを送る形にする。</summary>
    /// <param name="notice">知らせ。</param>
    /// <returns>送る形。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notice"/> が null。</exception>
    public static AnomalyNoticeDto ToDto(AnomalyNotice notice)
    {
        ArgumentNullException.ThrowIfNull(notice);
        return new AnomalyNoticeDto(notice.Change, ToDto(notice.Anomaly), notice.ShouldNotify);
    }

    /// <summary>送られてきた知らせを戻す。</summary>
    /// <param name="dto">送られてきた知らせ。</param>
    /// <returns>知らせ。知らない変化の種類や、扱えない異常なら null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dto"/> が null。</exception>
    public static AnomalyNotice? FromDto(AnomalyNoticeDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return Enum.IsDefined(dto.Change) && dto.Anomaly is not null && FromDto(dto.Anomaly) is { } anomaly
            ? new AnomalyNotice(dto.Change, anomaly, dto.ShouldNotify)
            : null;
    }

    /// <summary>設定を送る形にする。</summary>
    /// <param name="setting">設定。</param>
    /// <returns>送る形。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> が null。</exception>
    public static AnomalySettingDto ToDto(AnomalySetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);
        return new AnomalySettingDto(setting.Kind, setting.NotificationsEnabled, setting.ManualThreshold);
    }

    /// <summary>送られてきた設定を戻す。</summary>
    /// <param name="dto">送られてきた設定。</param>
    /// <returns>設定。知らない種類なら null。値の範囲は確かめない（<see cref="AnomalySettingRules.IsValid"/> で確かめる）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dto"/> が null。</exception>
    public static AnomalySetting? FromDto(AnomalySettingDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return Enum.IsDefined(dto.Kind) ? new AnomalySetting(dto.Kind, dto.NotificationsEnabled, dto.ManualThreshold) : null;
    }
}
