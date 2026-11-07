namespace Yamori.Core.Realtime.Collectors;

/// <summary>
/// 内蔵・外付けのドライブの、今の空き容量を集める。ネットワーク ドライブは含めない（読み取りで通信が起きるため）。
/// </summary>
public sealed class VolumeRealtimeCollector
{
    /// <summary>
    /// ドライブごとの今の空き容量を集める。
    /// </summary>
    /// <returns>ドライブごとの空き容量。名前の順。</returns>
    public IReadOnlyList<VolumeRealtime> Collect()
    {
        var volumes = new List<VolumeRealtime>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType is not (DriveType.Fixed or DriveType.Removable))
            {
                continue;
            }

            try
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                volumes.Add(new VolumeRealtime(
                    drive.Name,
                    string.IsNullOrWhiteSpace(drive.VolumeLabel) ? null : drive.VolumeLabel,
                    drive.DriveFormat,
                    (ulong)drive.TotalSize,
                    (ulong)drive.TotalFreeSpace));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // 取り外された直後のドライブなど。読めるドライブだけを表示する。
            }
        }

        return volumes.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
