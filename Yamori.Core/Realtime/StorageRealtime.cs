namespace Yamori.Core.Realtime;

/// <summary>
/// ストレージ（HDD・SSD）の今の状態。
/// </summary>
/// <param name="Disks">物理ディスクごとの状態。番号の順。</param>
/// <param name="Volumes">ドライブごとの空き容量。名前の順。</param>
public sealed record StorageRealtime(IReadOnlyList<DiskRealtime> Disks, IReadOnlyList<VolumeRealtime> Volumes);
