namespace Yamori.Core.Hardware;

/// <summary>
/// ドライブ文字のあるボリューム（例：C:）1 つの情報。
/// </summary>
/// <param name="DriveLetter">ドライブ文字（例：C）。</param>
/// <param name="SizeBytes">容量（バイト）。</param>
/// <param name="FreeBytes">空き容量（バイト）。</param>
/// <param name="FileSystem">ファイルシステム（例：NTFS）。分からない場合は null。</param>
public sealed record StorageVolumeInfo(char DriveLetter, ulong SizeBytes, ulong FreeBytes, string? FileSystem)
{
    /// <summary>
    /// 使用中の割合（0〜1）を取得する。
    /// </summary>
    /// <returns>容量が 0 の場合は 0。</returns>
    public double UsedRatio => SizeBytes == 0 ? 0 : (double)(SizeBytes - Math.Min(FreeBytes, SizeBytes)) / SizeBytes;
}
