using System;
using System.Globalization;
using Yamori.Core.Formatting;
using Yamori.Core.Hardware;
using Yamori.Localization;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// 部品の情報に出てくる種類や状態の値を、現在の UI カルチャの言語の文字列にする。
/// </summary>
public static class HardwareLabels
{
    /// <summary>
    /// ストレージの種類の表示名を取得する。
    /// </summary>
    /// <param name="type">ストレージの種類。</param>
    /// <returns>表示名（例：SSD）。</returns>
    public static string MediaType(StorageMediaType type) =>
        type switch
        {
            StorageMediaType.Ssd => "SSD",
            StorageMediaType.Hdd => "HDD",
            StorageMediaType.Scm => "SCM",
            _ => UiText.Get("Media_Unknown"),
        };

    /// <summary>
    /// ストレージの健康状態の表示名と意味を取得する。
    /// </summary>
    /// <param name="health">健康状態。</param>
    /// <returns>表示名と、どうすればよいかの説明。</returns>
    public static string Health(StorageHealth health) =>
        health switch
        {
            StorageHealth.Healthy => UiText.Get("Health_Healthy"),
            StorageHealth.Warning => UiText.Get("Health_Warning"),
            StorageHealth.Unhealthy => UiText.Get("Health_Unhealthy"),
            _ => UiText.Get("Health_Unknown"),
        };

    /// <summary>
    /// ファームウェアが報告する装置の状態の表示名を取得する。
    /// </summary>
    /// <param name="status">装置の状態。</param>
    /// <returns>表示名。</returns>
    public static string Status(DeviceStatus status) =>
        status switch
        {
            DeviceStatus.Ok => UiText.Get("Status_Ok"),
            DeviceStatus.NonCritical => UiText.Get("Status_NonCritical"),
            DeviceStatus.Critical => UiText.Get("Status_Critical"),
            DeviceStatus.NonRecoverable => UiText.Get("Status_NonRecoverable"),
            DeviceStatus.Other => UiText.Get("Status_Other"),
            _ => UiText.Get("Status_Unknown"),
        };

    /// <summary>
    /// 冷却装置の種類の表示名を取得する。
    /// </summary>
    /// <param name="type">冷却装置の種類。</param>
    /// <returns>表示名。</returns>
    public static string CoolingType(CoolingDeviceType type) =>
        type switch
        {
            CoolingDeviceType.Fan => UiText.Get("Cooling_Fan"),
            CoolingDeviceType.CentrifugalBlower => UiText.Get("Cooling_Blower"),
            CoolingDeviceType.ChipFan => UiText.Get("Cooling_ChipFan"),
            CoolingDeviceType.CabinetFan => UiText.Get("Cooling_CaseFan"),
            CoolingDeviceType.PowerSupplyFan => UiText.Get("Cooling_PowerSupplyFan"),
            CoolingDeviceType.HeatPipe => UiText.Get("Cooling_HeatPipe"),
            CoolingDeviceType.IntegratedRefrigeration => UiText.Get("Cooling_Refrigeration"),
            CoolingDeviceType.ActiveCooling => UiText.Get("Cooling_Active"),
            CoolingDeviceType.PassiveCooling => UiText.Get("Cooling_Passive"),
            CoolingDeviceType.Other => UiText.Get("Cooling_Other"),
            _ => UiText.Get("Cooling_Unknown"),
        };

    /// <summary>
    /// 電源の方式の表示名を取得する。
    /// </summary>
    /// <param name="kind">電源の方式。</param>
    /// <returns>表示名。分からない場合は null（項目ごと表示しない）。</returns>
    public static string? SupplyKind(PowerSupplyKind kind) =>
        kind switch
        {
            PowerSupplyKind.Switching => UiText.Get("Psu_Switching"),
            PowerSupplyKind.Linear => UiText.Get("Psu_Linear"),
            PowerSupplyKind.Battery => UiText.Get("Psu_Battery"),
            PowerSupplyKind.Ups => UiText.Get("Psu_Ups"),
            PowerSupplyKind.Converter => UiText.Get("Psu_Converter"),
            PowerSupplyKind.Regulator => UiText.Get("Psu_Regulator"),
            PowerSupplyKind.Other => UiText.Get("Psu_Other"),
            _ => null,
        };

    /// <summary>
    /// 今使っている電源の表示名を取得する。
    /// </summary>
    /// <param name="source">今使っている電源。</param>
    /// <returns>表示名。</returns>
    public static string Source(PowerSource source) =>
        source switch
        {
            PowerSource.Ac => UiText.Get("Source_Ac"),
            PowerSource.Battery => UiText.Get("Source_Battery"),
            _ => UiText.Get("Source_Unknown"),
        };

    /// <summary>
    /// 筐体の形の表示名を取得する。
    /// </summary>
    /// <param name="chassis">筐体の形。</param>
    /// <returns>表示名。分からない場合は null（項目ごと表示しない）。</returns>
    public static string? Chassis(ChassisKind chassis) =>
        chassis switch
        {
            ChassisKind.Desktop => UiText.Get("Chassis_Desktop"),
            ChassisKind.Laptop => UiText.Get("Chassis_Laptop"),
            ChassisKind.Convertible => UiText.Get("Chassis_Convertible"),
            ChassisKind.Tablet => UiText.Get("Chassis_Tablet"),
            ChassisKind.AllInOne => UiText.Get("Chassis_AllInOne"),
            ChassisKind.MiniPc => UiText.Get("Chassis_MiniPc"),
            ChassisKind.Server => UiText.Get("Chassis_Server"),
            ChassisKind.Other => UiText.Get("Chassis_Other"),
            _ => null,
        };

    /// <summary>
    /// ネットワークの接続方式の表示名を取得する。
    /// </summary>
    /// <param name="kind">接続方式。</param>
    /// <returns>表示名。</returns>
    public static string ConnectionKind(NetworkConnectionKind kind) =>
        kind switch
        {
            NetworkConnectionKind.Ethernet => UiText.Get("Net_Ethernet"),
            NetworkConnectionKind.WiFi => UiText.Get("Net_WiFi"),
            NetworkConnectionKind.Cellular => UiText.Get("Net_Cellular"),
            NetworkConnectionKind.Bluetooth => UiText.Get("Net_Bluetooth"),
            NetworkConnectionKind.Other => UiText.Get("Net_Other"),
            _ => UiText.Get("Net_Unknown"),
        };

    /// <summary>
    /// リンク速度を、1 Gbps 以上なら Gbps、1 Mbps 以上なら Mbps、それ未満なら kbps で表す（1000 進）。
    /// </summary>
    /// <param name="bitsPerSecond">リンク速度（ビット／秒）。</param>
    /// <returns>例：<c>2.5 Gbps</c>。null か 0 以下の場合は null。</returns>
    public static string? LinkSpeed(double? bitsPerSecond) =>
        bitsPerSecond switch
        {
            null or <= 0 => null,
            >= 1e9 => UiText.Format("Hw_Value_Gbps", bitsPerSecond.Value / 1e9),
            >= 1e6 => UiText.Format("Hw_Value_Mbps", bitsPerSecond.Value / 1e6),
            _ => UiText.Format("Hw_Value_Kbps", bitsPerSecond.Value / 1e3),
        };

    /// <summary>
    /// バイト数を単位つきの文字列にする。
    /// </summary>
    /// <param name="bytes">バイト数。null の場合は null。</param>
    /// <returns>例：<c>16 GB</c>。</returns>
    public static string? Bytes(ulong? bytes) => bytes is { } value ? ByteSizeFormatter.Format(value) : null;

    /// <summary>
    /// 日付を、現在の UI カルチャの短い形式にする。
    /// </summary>
    /// <param name="date">日付。null の場合は null。</param>
    /// <returns>例：<c>2024/10/04</c>。</returns>
    public static string? Date(DateOnly? date) => date?.ToString("d", CultureInfo.CurrentUICulture);
}
