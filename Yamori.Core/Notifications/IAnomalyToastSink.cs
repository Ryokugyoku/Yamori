using Yamori.Core.Anomalies;

namespace Yamori.Core.Notifications;

/// <summary>
/// 異常の通知を実際に出す先（通知専用の常駐プログラムでは、UI スレッドで Windows の通知を出す）。
/// </summary>
public interface IAnomalyToastSink
{
    /// <summary>
    /// 一時停止中・全画面中でなければ、異常の通知を出す。
    /// </summary>
    /// <param name="anomaly">異常。</param>
    /// <returns>出したら true。一時停止中・全画面中で控えた、または出せなかったら false。</returns>
    Task<bool> TryShowAsync(AnomalyRecord anomaly);
}
