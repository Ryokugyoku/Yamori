namespace Yamori.Core.Sensors.Ipc;

/// <summary>
/// <see cref="SensorNotificationTopics"/> の補助。
/// </summary>
public static class SensorNotificationTopicsExtensions
{
    /// <summary>
    /// 選んだ種類に、通知が含まれるかどうかを調べる。
    /// </summary>
    /// <param name="topics">選んだ種類。<see cref="SensorNotificationTopics.All"/> ならすべてを含む。</param>
    /// <param name="kind">通知の種類。</param>
    /// <returns>含まれるなら true。知らない種類の通知は、すべてを選んだときだけ含む。</returns>
    public static bool Includes(this SensorNotificationTopics topics, SensorNotificationKind kind) =>
        topics == SensorNotificationTopics.All || (topics & TopicOf(kind)) != 0;

    /// <summary>
    /// 通知の種類が属する種類を取得する。
    /// </summary>
    /// <param name="kind">通知の種類。</param>
    /// <returns>属する種類。知らない通知なら <see cref="SensorNotificationTopics.All"/>（選んだ種類には含まれない）。</returns>
    public static SensorNotificationTopics TopicOf(SensorNotificationKind kind) => kind switch
    {
        SensorNotificationKind.ReadingsUpdated => SensorNotificationTopics.Readings,
        SensorNotificationKind.CatalogChanged => SensorNotificationTopics.Catalog,
        SensorNotificationKind.ExtendedSensorStateChanged => SensorNotificationTopics.ExtendedState,
        SensorNotificationKind.AnomalyChanged => SensorNotificationTopics.Anomalies,
        _ => SensorNotificationTopics.All,
    };
}
