namespace Yamori.Controls;

/// <summary>
/// <see cref="TimeSeriesChart"/> に点線で引く目安の値（注意の温度など）。
/// </summary>
/// <param name="Value">値。</param>
/// <param name="SeriesIndex">目盛りを使う系列の番号（<see cref="ChartData.Series"/> の中の位置）。</param>
/// <param name="Label">線の横に書く説明。</param>
public sealed record ChartReferenceLine(double Value, int SeriesIndex, string Label);
