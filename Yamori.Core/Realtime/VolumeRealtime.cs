namespace Yamori.Core.Realtime;

/// <summary>
/// ドライブ（ボリューム）1 つの今の空き容量。
/// </summary>
/// <param name="Name">ドライブの名前（例：<c>C:\</c>）。</param>
/// <param name="Label">ボリュームのラベル。ない場合は null。</param>
/// <param name="FileSystem">ファイル システム（例：<c>NTFS</c>）。</param>
/// <param name="TotalBytes">容量（バイト）。</param>
/// <param name="FreeBytes">空き容量（バイト）。</param>
public sealed record VolumeRealtime(string Name, string? Label, string FileSystem, ulong TotalBytes, ulong FreeBytes)
{
    /// <summary>使用率（%）を取得する。</summary>
    public double UsagePercent => TotalBytes == 0 ? 0 : 100.0 * (TotalBytes - FreeBytes) / TotalBytes;
}
