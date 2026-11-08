using System;
using System.Collections.Generic;
using Yamori.Core.Analysis.Parts;
using Yamori.Core.Sensors.Service;

namespace Yamori.Controls;

/// <summary>
/// <see cref="TimeSeriesChart"/> に重ねて描く 1 本の系列。系列ごとに自分の目盛りを持つ。
/// </summary>
/// <param name="Name">凡例に出す名前。</param>
/// <param name="Points">点（古い順。最小・最大の幅を薄く塗り、平均を線で描く）。</param>
/// <param name="Scale">値の目盛り。</param>
/// <param name="FormatValue">値を表示用の文字列にする処理（軸の目盛りと、指したところの値に使う）。</param>
/// <param name="ColorIndex">色の番号（0〜2。<see cref="TimeSeriesChart.Series1Brush"/> から順に使う）。</param>
/// <param name="AxisSide">目盛りを書く軸。</param>
/// <param name="MaxGap">点の間がこれより空いたら線を切る（記録が途切れた区間をつながないため）。</param>
public sealed record ChartSeries(
    string Name,
    IReadOnlyList<SensorHistoryPoint> Points,
    ChartAxisScale Scale,
    Func<double, string> FormatValue,
    int ColorIndex,
    ChartAxisSide AxisSide,
    TimeSpan MaxGap);
