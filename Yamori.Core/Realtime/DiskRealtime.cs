namespace Yamori.Core.Realtime;

/// <summary>
/// 物理ディスク 1 台の今の状態。測れなかった値は null。
/// </summary>
/// <param name="DiskNumber">物理ディスクの番号（ディスクの管理での「ディスク 0」などの番号）。</param>
/// <param name="Name">製品名。分からない場合は null。</param>
/// <param name="DriveLetters">このディスクにあるドライブ（例：<c>C: D:</c>）。ない場合は空。</param>
public sealed record DiskRealtime(int DiskNumber, string? Name, string DriveLetters)
{
    /// <summary>アクティブな時間の割合（%）を取得する。100 に張り付いていると、ディスクが追いついていない。</summary>
    public double? ActiveTimePercent { get; init; }

    /// <summary>読み込みの速さ（バイト／秒）を取得する。</summary>
    public double? ReadBytesPerSecond { get; init; }

    /// <summary>書き込みの速さ（バイト／秒）を取得する。</summary>
    public double? WriteBytesPerSecond { get; init; }

    /// <summary>1 秒あたりの読み込み回数を取得する。</summary>
    public double? ReadsPerSecond { get; init; }

    /// <summary>1 秒あたりの書き込み回数を取得する。</summary>
    public double? WritesPerSecond { get; init; }

    /// <summary>1 回の読み込みにかかった平均時間（ミリ秒）を取得する。</summary>
    public double? ReadLatencyMilliseconds { get; init; }

    /// <summary>1 回の書き込みにかかった平均時間（ミリ秒）を取得する。</summary>
    public double? WriteLatencyMilliseconds { get; init; }

    /// <summary>処理を待っている要求の数を取得する。</summary>
    public double? QueueLength { get; init; }

    /// <summary>温度（℃）を取得する。WMI か、ディスクへの直接の問い合わせ（IOCTL）で読めた場合だけ。</summary>
    public int? TemperatureCelsius { get; init; }
}
