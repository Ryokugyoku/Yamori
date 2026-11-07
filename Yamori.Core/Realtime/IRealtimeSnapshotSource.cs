namespace Yamori.Core.Realtime;

/// <summary>
/// 部品ごとの今の状態を、まとめて 1 回測る。
/// </summary>
public interface IRealtimeSnapshotSource : IDisposable
{
    /// <summary>
    /// 今の状態を測る。1 秒あたりの値や使用率は前回の測定との差から計算するため、最初の 1 回では分からない値がある。
    /// </summary>
    /// <returns>測った結果。</returns>
    RealtimeSnapshot Take();
}
