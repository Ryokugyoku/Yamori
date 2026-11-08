namespace Yamori.Core.Anomalies;

/// <summary>
/// 異常の判断の理由の数値（<see cref="AnomalyRecord.Evidence"/>）を読む。通知の文面と、分析画面の説明で共有する。
/// </summary>
public static class AnomalyEvidence
{
    /// <summary>
    /// 判断の理由の数値を取り出す。
    /// </summary>
    /// <param name="anomaly">異常。</param>
    /// <param name="name">数値の名前（<see cref="AnomalyEvidenceNames"/>）。</param>
    /// <returns>値。なければ、または有限でなければ null。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static double? Get(AnomalyRecord anomaly, string name)
    {
        ArgumentNullException.ThrowIfNull(anomaly);
        ArgumentNullException.ThrowIfNull(name);
        return anomaly.Evidence.TryGetValue(name, out var value) && double.IsFinite(value) ? value : null;
    }

    /// <summary>
    /// サーマルスロットリングのうち、熱の制限の印も注意の温度以上の時間もなく、電力・電流の制限だけがかかっていたものかどうかを調べる。
    /// 理由の数値がなければ熱とみなす。
    /// </summary>
    /// <param name="anomaly">異常。</param>
    /// <returns>電力・電流の制限だけなら true。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="anomaly"/> が null。</exception>
    public static bool IsLimitOnlyThrottling(AnomalyRecord anomaly)
    {
        ArgumentNullException.ThrowIfNull(anomaly);
        return anomaly.Kind == AnomalyKind.ThermalThrottling
            && Get(anomaly, AnomalyEvidenceNames.ThermalLimitSeconds) is 0.0
            && Get(anomaly, AnomalyEvidenceNames.AboveThresholdSeconds) is 0.0
            && (Get(anomaly, AnomalyEvidenceNames.PowerLimitSeconds) > 0 || Get(anomaly, AnomalyEvidenceNames.CurrentLimitSeconds) > 0);
    }
}
