namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// 物理ディスクの番号から、製品名を調べる。
/// </summary>
public interface IDiskNameSource
{
    /// <summary>
    /// 物理ディスクの番号ごとの製品名を読み取る。
    /// </summary>
    /// <returns>ディスクの番号（PhysicalDrive の番号）をキーにした製品名。読めない場合は空。</returns>
    IReadOnlyDictionary<int, string> ReadByDiskNumber();
}
