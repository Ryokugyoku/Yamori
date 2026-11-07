using System;
using System.Collections.Generic;
using System.Linq;
using Yamori.Core.Hardware;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// ハードウェア一覧を、部品の種類ごとの表示内容の一覧にする。並び順は <see cref="HardwareComponent"/> の順。
/// </summary>
public sealed class HardwareSectionsPresenter
{
    private readonly IReadOnlyList<IHardwareSectionBuilder> _builders;

    /// <summary>
    /// 部品の種類ごとの処理を指定して作る。
    /// </summary>
    /// <param name="builders">部品の種類ごとの表示内容を作る処理。</param>
    /// <exception cref="ArgumentNullException"><paramref name="builders"/> が null。</exception>
    public HardwareSectionsPresenter(IEnumerable<IHardwareSectionBuilder> builders)
    {
        ArgumentNullException.ThrowIfNull(builders);
        _builders = builders.OrderBy(b => b.Component).ToArray();
    }

    /// <summary>
    /// すべての部品の種類を受け持つ、標準の構成で作る。
    /// </summary>
    /// <returns>標準の構成の <see cref="HardwareSectionsPresenter"/>。</returns>
    public static HardwareSectionsPresenter CreateDefault() =>
        new(
        [
            new CpuSectionBuilder(),
            new MotherboardSectionBuilder(),
            new MemorySectionBuilder(),
            new GpuSectionBuilder(),
            new StorageSectionBuilder(),
            new FanSectionBuilder(),
            new PowerSupplySectionBuilder(),
            new NetworkSectionBuilder(),
        ]);

    /// <summary>
    /// 表示内容の一覧を作る。言語は現在の UI カルチャに従う。
    /// </summary>
    /// <param name="inventory">ハードウェア一覧。</param>
    /// <returns>部品の種類ごとの表示内容。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inventory"/> が null。</exception>
    public IReadOnlyList<HardwareSection> Build(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        return _builders.Select(b => b.Build(inventory)).ToArray();
    }
}
