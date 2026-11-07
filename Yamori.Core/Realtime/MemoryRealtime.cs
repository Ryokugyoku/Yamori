namespace Yamori.Core.Realtime;

/// <summary>
/// メモリ（RAM）の今の状態。測れなかった値は null。
/// </summary>
public sealed record MemoryRealtime
{
    /// <summary>物理メモリの合計（バイト）を取得する。</summary>
    public ulong? TotalBytes { get; init; }

    /// <summary>使用中の物理メモリ（バイト）を取得する。</summary>
    public ulong? UsedBytes { get; init; }

    /// <summary>すぐに使える物理メモリ（バイト）を取得する。スタンバイのキャッシュを含む。</summary>
    public ulong? AvailableBytes { get; init; }

    /// <summary>物理メモリの使用率（%）を取得する。</summary>
    public double? UsagePercent { get; init; }

    /// <summary>今のコミット量（バイト）を取得する。</summary>
    public ulong? CommittedBytes { get; init; }

    /// <summary>コミットの上限（バイト）を取得する。</summary>
    public ulong? CommitLimitBytes { get; init; }

    /// <summary>起動してからのコミット量の最大（バイト）を取得する。</summary>
    public ulong? CommitPeakBytes { get; init; }

    /// <summary>システムのファイル キャッシュ（バイト）を取得する。</summary>
    public ulong? SystemCacheBytes { get; init; }

    /// <summary>スタンバイのキャッシュ（バイト）を取得する。必要になればすぐに空けられる。</summary>
    public ulong? StandbyBytes { get; init; }

    /// <summary>変更済みでディスクへの書き出しを待っているメモリ（バイト）を取得する。</summary>
    public ulong? ModifiedBytes { get; init; }

    /// <summary>何にも使われていないメモリ（バイト）を取得する。</summary>
    public ulong? FreeBytes { get; init; }

    /// <summary>カーネルのページング プール（バイト）を取得する。</summary>
    public ulong? PagedPoolBytes { get; init; }

    /// <summary>カーネルの非ページング プール（バイト）を取得する。</summary>
    public ulong? NonPagedPoolBytes { get; init; }

    /// <summary>1 秒あたりのページ フォールトの回数を取得する。</summary>
    public double? PageFaultsPerSecond { get; init; }

    /// <summary>1 秒あたりにディスクとやり取りしたページの数を取得する。多い状態が続くとメモリ不足。</summary>
    public double? PagesPerSecond { get; init; }

    /// <summary>ページ ファイルの使用率（%）を取得する。</summary>
    public double? PagingFileUsagePercent { get; init; }

    /// <summary>メモリ（DRAM）の消費電力（W）を取得する。</summary>
    public double? PowerWatts { get; init; }
}
