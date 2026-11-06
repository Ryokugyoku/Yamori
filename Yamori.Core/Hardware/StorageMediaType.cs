namespace Yamori.Core.Hardware;

/// <summary>
/// ストレージの種類。値は MSFT_PhysicalDisk.MediaType に合わせている。
/// </summary>
public enum StorageMediaType
{
    /// <summary>種類が分からない。</summary>
    Unknown = 0,

    /// <summary>ハードディスク（HDD）。</summary>
    Hdd = 3,

    /// <summary>ソリッドステートドライブ（SSD）。</summary>
    Ssd = 4,

    /// <summary>ストレージクラスメモリ（SCM）。</summary>
    Scm = 5,
}
