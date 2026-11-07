namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// 物理ディスクの番号ごとに、再起動しても変わらない識別子（シリアル番号など）を読み取る。
/// </summary>
public interface IDiskIdentitySource
{
    /// <summary>
    /// 物理ディスクの番号ごとの識別子を読み取る。
    /// </summary>
    /// <returns>ディスクの番号から識別子への対応。読めなかったディスクは含めない。読めなければ空。</returns>
    IReadOnlyDictionary<int, string> ReadByDiskNumber();
}
