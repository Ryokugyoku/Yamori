namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// Windows 全体のメモリの使い方と、プロセス・スレッド・ハンドルの数。
/// </summary>
/// <param name="TotalPhysicalBytes">物理メモリの合計（バイト）。</param>
/// <param name="AvailablePhysicalBytes">すぐに使える物理メモリ（バイト）。スタンバイのキャッシュを含む。</param>
/// <param name="MemoryLoadPercent">物理メモリの使用率（%）。</param>
/// <param name="CommittedBytes">今のコミット量（バイト）。アプリが確保を約束されたメモリの合計。</param>
/// <param name="CommitLimitBytes">コミットの上限（バイト）。物理メモリとページ ファイルの合計。</param>
/// <param name="CommitPeakBytes">起動してからのコミット量の最大（バイト）。</param>
/// <param name="SystemCacheBytes">システムのファイル キャッシュ（バイト）。</param>
/// <param name="KernelPagedBytes">カーネルのページング プール（バイト）。</param>
/// <param name="KernelNonpagedBytes">カーネルの非ページング プール（バイト）。</param>
/// <param name="ProcessCount">プロセスの数。</param>
/// <param name="ThreadCount">スレッドの数。</param>
/// <param name="HandleCount">開かれているハンドルの数。</param>
/// <param name="Uptime">Windows を起動してからの時間。</param>
public sealed record SystemPerformance(
    ulong TotalPhysicalBytes,
    ulong AvailablePhysicalBytes,
    double MemoryLoadPercent,
    ulong CommittedBytes,
    ulong CommitLimitBytes,
    ulong CommitPeakBytes,
    ulong SystemCacheBytes,
    ulong KernelPagedBytes,
    ulong KernelNonpagedBytes,
    int ProcessCount,
    int ThreadCount,
    int HandleCount,
    TimeSpan Uptime);
