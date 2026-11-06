namespace Yamori.Core.Hardware;

/// <summary>
/// PC のハードウェア一覧を提供する。
/// </summary>
public interface IHardwareInventoryProvider
{
    /// <summary>
    /// 現在のハードウェア一覧を取得する。一部の部品が取得できなくても、取得できた分は返す。
    /// </summary>
    /// <returns>ハードウェア一覧。</returns>
    HardwareInventory GetInventory();
}
