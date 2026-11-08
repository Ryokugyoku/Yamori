namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 判定する直近の期間の、区切り 1 つの判定の材料。
/// </summary>
/// <param name="StartUtc">区切りの始まり。</param>
/// <param name="Temperature">待機中の温度とふだん（負荷の帯ごとの差を、分の数で重みをつけて平均したもの）。判定できなければ null。</param>
/// <param name="IdleLoad">待機中の負荷とふだん。判定できなければ null。</param>
public sealed record UsualBucketResult(DateTimeOffset StartUtc, UsualMetricValue? Temperature, UsualMetricValue? IdleLoad);
