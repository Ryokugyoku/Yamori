using System;
using System.Collections.Generic;
using System.Linq;
using Yamori.Core.Formatting;
using Yamori.Core.Hardware;
using Yamori.Localization;

namespace Yamori.Presentation;

/// <summary>
/// ハードウェア一覧から、概要画面の一番上に出す主な仕様（CPU・メモリ・GPU・ストレージ）を作る。言語は現在の UI カルチャに従う。
/// 取得できなかった部品は出さない。
/// </summary>
public sealed class SystemSpecPresenter
{
    /// <summary>
    /// 主な仕様を作る。
    /// </summary>
    /// <param name="inventory">ハードウェア一覧。</param>
    /// <returns>表示する順の仕様。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inventory"/> が null。</exception>
    public IReadOnlyList<SpecItem> Build(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        var items = new List<SpecItem>();
        if (inventory.Cpu is { } cpu)
        {
            items.Add(new SpecItem(
                HardwareGlyphs.For(HardwareComponent.Cpu),
                ComponentDisplayNames.Get(HardwareComponent.Cpu),
                cpu.Name,
                UiText.Format("Spec_CpuDetail", cpu.PhysicalCores, cpu.LogicalProcessors)));
        }

        if (inventory.Memory is { TotalBytes: > 0 } memory)
        {
            var count = UiText.Format("Spec_ModuleCount", memory.ModuleCount);
            items.Add(new SpecItem(
                HardwareGlyphs.For(HardwareComponent.Memory),
                ComponentDisplayNames.Get(HardwareComponent.Memory),
                ByteSizeFormatter.Format(memory.TotalBytes),
                memory.TypeName is { } type ? UiText.Format("Spec_Joined", type, count) : count));
        }

        // 専用の GPU があればそれを、なければ CPU の内蔵 GPU を出す。
        if ((inventory.Gpus.FirstOrDefault() ?? inventory.Cpu?.IntegratedGpu) is { } gpu)
        {
            items.Add(new SpecItem(
                HardwareGlyphs.For(HardwareComponent.Gpu),
                ComponentDisplayNames.Get(HardwareComponent.Gpu),
                gpu.Name,
                gpu.DedicatedMemoryBytes is { } vram and > 0 ? UiText.Format("Spec_GpuDetail", ByteSizeFormatter.Format(vram)) : string.Empty));
        }

        if (inventory.Storage.Count > 0)
        {
            var total = inventory.Storage.Aggregate(0UL, (sum, s) => sum + s.SizeBytes);
            items.Add(new SpecItem(
                HardwareGlyphs.For(HardwareComponent.Storage),
                ComponentDisplayNames.Get(HardwareComponent.Storage),
                ByteSizeFormatter.Format(total),
                UiText.Format("Spec_StorageDetail", inventory.Storage.Count)));
        }

        return items;
    }
}
