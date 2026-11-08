namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 待機中の負荷を帯に分ける。温度は同じ帯（同じくらいの負荷）の時間どうしで比べる。
/// </summary>
public static class LoadBands
{
    /// <summary>
    /// 負荷の帯の番号を求める。
    /// </summary>
    /// <param name="loadPercent">負荷（%）。</param>
    /// <param name="options">決まりごと。</param>
    /// <returns>帯の番号（0 から。境目は <see cref="UsualComparisonOptions.LoadBandEdges"/>）。待機中でなければ（上限以上か、値が不正なら）null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> が null。</exception>
    public static int? Of(double loadPercent, UsualComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!double.IsFinite(loadPercent) || loadPercent >= options.IdleLoadCeilingPercent)
        {
            return null;
        }

        var band = 0;
        while (band < options.LoadBandEdges.Count && loadPercent >= options.LoadBandEdges[band])
        {
            band++;
        }

        return band;
    }
}
