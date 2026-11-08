namespace Yamori.Core.Sensors.Service;

/// <summary>
/// GUI と Helper の間のセンサーのやり取り（<see cref="ISensorService"/>）の版。版がずれても動くよう、
/// 項目の追加は後ろに行い、既存の項目の意味と列挙値の番号は変えない。
/// </summary>
public static class SensorContract
{
    /// <summary>
    /// 今の版。2 でアプリごとの CPU 使用率の記録（<see cref="Ipc.SensorOperation.GetAppCpuUsage"/>）を、
    /// 3 で異常の一覧・設定の操作と異常の知らせ（<see cref="Ipc.SensorNotificationKind.AnomalyChanged"/>）、受け取る通知の選択
    /// （<see cref="Ipc.IpcEnvelope.Topics"/>）、1 秒ごとの記録（<see cref="TimeSeries.TimeSeriesResolution.OneSecond"/>）、
    /// オン・オフの印のセンサー（<see cref="SensorType.Flag"/>）を、4 で異常の通知の全体のオン・オフ
    /// （<see cref="Ipc.SensorOperation.GetAnomalyNotificationsEnabled"/>）を足した。
    /// </summary>
    public const int Version = 4;

    /// <summary>やり取りできる最も古い版。これより古い相手とは接続しない。</summary>
    public const int MinimumCompatibleVersion = 1;

    /// <summary>
    /// 相手の版とやり取りできるかどうかを調べる。
    /// </summary>
    /// <param name="peerVersion">相手の版。</param>
    /// <returns>やり取りできれば true。新しい版の相手とは、知っている項目だけでやり取りする。</returns>
    public static bool IsCompatible(int peerVersion) => peerVersion >= MinimumCompatibleVersion;
}
