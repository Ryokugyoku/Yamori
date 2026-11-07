using Yamori.Core.Hardware;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// ハードウェア一覧から、部品の種類 1 つ分の表示内容を作る。
/// </summary>
public interface IHardwareSectionBuilder
{
    /// <summary>この処理が受け持つ部品の種類。</summary>
    HardwareComponent Component { get; }

    /// <summary>
    /// 表示内容を作る。言語は現在の UI カルチャに従う。
    /// </summary>
    /// <param name="inventory">ハードウェア一覧。</param>
    /// <returns>部品の種類 1 つ分の表示内容。</returns>
    HardwareSection Build(HardwareInventory inventory);
}
