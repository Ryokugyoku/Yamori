using System.Runtime.InteropServices;

namespace Yamori.Core.Interop;

/// <summary>
/// パフォーマンスカウンター（PDH: Performance Data Helper）の API の宣言。管理者権限を必要としない。
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>PDH の成功を表す戻り値。</summary>
    internal const uint PdhSuccess = 0;

    /// <summary>バッファーが足りないことを表す戻り値（PDH_MORE_DATA）。</summary>
    internal const uint PdhMoreData = 0x800007D2;

    /// <summary>値を倍精度浮動小数点数で受け取る書式（PDH_FMT_DOUBLE）。</summary>
    internal const uint PdhFormatDouble = 0x00000200;

    /// <summary>100 を超える値を 100 に丸めない書式（PDH_FMT_NOCAP100）。CPU の動作クロックの割合などで使う。</summary>
    internal const uint PdhFormatNoCap100 = 0x00008000;

    /// <summary>PDH_FMT_COUNTERVALUE_ITEM_W 1 件の大きさ（バイト）。x86・x64・ARM64 のいずれでも 24。</summary>
    internal const int PdhFormattedItemSize = 24;

    /// <summary>PDH_FMT_COUNTERVALUE_ITEM_W の中の CStatus の位置（バイト）。</summary>
    internal const int PdhFormattedItemStatusOffset = 8;

    /// <summary>PDH_FMT_COUNTERVALUE_ITEM_W の中の doubleValue の位置（バイト）。</summary>
    internal const int PdhFormattedItemValueOffset = 16;

    /// <summary>
    /// カウンターをまとめて読むためのクエリを開く。
    /// </summary>
    /// <param name="dataSource">ログファイル名。リアルタイムのデータを読む場合は null。</param>
    /// <param name="userData">呼び出し側で使う値。使わない場合は 0。</param>
    /// <param name="query">開いたクエリのハンドル。</param>
    /// <returns>PDH の状態コード。成功は <see cref="PdhSuccess"/>。</returns>
    [LibraryImport("pdh.dll", EntryPoint = "PdhOpenQueryW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint PdhOpenQuery(string? dataSource, nint userData, out nint query);

    /// <summary>
    /// 英語名のパスでカウンターをクエリに加える。Windows の表示言語に関係なく同じパスで指定できる。
    /// </summary>
    /// <param name="query">クエリのハンドル。</param>
    /// <param name="fullCounterPath">カウンターのパス（例：<c>\Processor Information(*)\% Processor Time</c>）。</param>
    /// <param name="userData">呼び出し側で使う値。使わない場合は 0。</param>
    /// <param name="counter">加えたカウンターのハンドル。</param>
    /// <returns>PDH の状態コード。成功は <see cref="PdhSuccess"/>。</returns>
    [LibraryImport("pdh.dll", EntryPoint = "PdhAddEnglishCounterW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint PdhAddEnglishCounter(nint query, string fullCounterPath, nint userData, out nint counter);

    /// <summary>
    /// クエリに加えたすべてのカウンターの今の値を取り込む。
    /// </summary>
    /// <param name="query">クエリのハンドル。</param>
    /// <returns>PDH の状態コード。成功は <see cref="PdhSuccess"/>。</returns>
    [LibraryImport("pdh.dll")]
    internal static partial uint PdhCollectQueryData(nint query);

    /// <summary>
    /// ワイルドカードを含むカウンターの値を、インスタンスごとの一覧として取得する。
    /// </summary>
    /// <param name="counter">カウンターのハンドル。</param>
    /// <param name="format">値の書式（<see cref="PdhFormatDouble"/> など）。</param>
    /// <param name="bufferSize">バッファーの大きさ（バイト）。足りなければ必要な大きさが返る。</param>
    /// <param name="itemCount">取得した件数。</param>
    /// <param name="itemBuffer">結果を受け取るバッファー。大きさを調べるだけの場合は 0。</param>
    /// <returns>PDH の状態コード。バッファーが足りなければ <see cref="PdhMoreData"/>。</returns>
    [LibraryImport("pdh.dll", EntryPoint = "PdhGetFormattedCounterArrayW")]
    internal static partial uint PdhGetFormattedCounterArray(nint counter, uint format, ref uint bufferSize, out uint itemCount, nint itemBuffer);

    /// <summary>
    /// クエリを閉じ、加えたカウンターもすべて解放する。
    /// </summary>
    /// <param name="query">クエリのハンドル。</param>
    /// <returns>PDH の状態コード。成功は <see cref="PdhSuccess"/>。</returns>
    [LibraryImport("pdh.dll")]
    internal static partial uint PdhCloseQuery(nint query);
}
