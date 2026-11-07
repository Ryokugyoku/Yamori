using System;
using System.Linq;
using Yamori.Core.Hardware;
using Yamori.Localization;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// ストレージの表示内容を作る。ドライブごとの空き容量と、取得できた場合は温度・消耗度も表示する。
/// </summary>
public sealed class StorageSectionBuilder : IHardwareSectionBuilder
{
    /// <inheritdoc />
    public HardwareComponent Component => HardwareComponent.Storage;

    /// <inheritdoc />
    public HardwareSection Build(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var entries = inventory.Storage.Select(CreateEntry).ToArray();
        return HardwareSections.Create(Component, entries, "Hw_Storage_Note");
    }

    private static HardwareEntry CreateEntry(StorageDeviceInfo disk)
    {
        var facts = new HardwareFactList().Add("Hw_Fact_Health", HardwareLabels.Health(disk.Health));
        foreach (var volume in disk.Volumes)
        {
            facts.Add(
                "Hw_Fact_Drive",
                UiText.Format(
                    "Hw_Storage_VolumeUsage",
                    volume.DriveLetter,
                    HardwareLabels.Bytes(volume.FreeBytes),
                    HardwareLabels.Bytes(volume.SizeBytes),
                    volume.UsedRatio * 100));
        }

        if (disk.Reliability is { } reliability)
        {
            facts
                .Add("Hw_Fact_Temperature", reliability.TemperatureCelsius is { } t ? UiText.Format("Hw_Value_Celsius", t) : null)
                .Add("Hw_Fact_Wear", reliability.WearPercent is { } w ? UiText.Format("Hw_Storage_Wear", w) : null)
                .Add("Hw_Fact_PowerOnHours", reliability.PowerOnHours is { } h ? UiText.Format("Hw_Value_Hours", h) : null);
        }

        facts
            .Add("Hw_Fact_RotationSpeed", disk.SpindleSpeedRpm is { } rpm ? UiText.Format("Hw_Value_Rpm", rpm) : null)
            .Add("Hw_Fact_Firmware", disk.FirmwareVersion);

        return new HardwareEntry(
            disk.Model,
            HardwareSections.Join(
                HardwareLabels.Bytes(disk.SizeBytes),
                UiText.Format("Hw_Storage_MediaBus", HardwareLabels.MediaType(disk.MediaType), disk.BusType)),
            facts.ToList());
    }
}
