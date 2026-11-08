using System;
using Yamori.Core.Sensors;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// 値を大きく見せるカード（概要のサマリーカード、リアルタイムの主な値）に、最新値と直近の動きを書き込む。
/// </summary>
public static class SensorCardFiller
{
    /// <summary>
    /// 値・割合・不透明度・折れ線グラフを書き込む。見出しやアイコンは呼び出し側で決める。
    /// </summary>
    /// <param name="item">書き込む先のカード。</param>
    /// <param name="board">センサーの今の状態。</param>
    /// <param name="descriptor">表示するセンサーの説明。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static void Fill(SummaryCardItem item, SensorBoard board, SensorDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(descriptor);

        var key = descriptor.Key;
        var (text, percent, opacity) = SensorRowFiller.Describe(descriptor, board.GetLatest(key), board.GetState(key));
        item.ValueText = text;
        item.Percent = percent ?? 0;
        item.HasPercent = percent is not null;
        item.Opacity = opacity;

        // 割合の値は 0〜100 の目盛りで描く（小さな揺れを大きく見せて不安をあおらないよう、目盛りを値に合わせて広げない）。
        var isPercent = percent is not null;
        item.TrendMinimum = isPercent ? 0 : double.NaN;
        item.TrendMaximum = isPercent ? 100 : double.NaN;
        item.Trend = board.GetTrend(key);
    }
}
