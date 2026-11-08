using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Yamori.Core.Analysis.Parts;
using Yamori.Core.Sensors.Service;

namespace Yamori.Controls;

/// <summary>
/// 時間の軸に、複数の系列（負荷・温度・クロックなど）を重ねて描く折れ線グラフ。系列ごとに自分の目盛りを持ち、
/// 左右の軸にそれぞれ 1 つずつ目盛りを書く。異常の区間に色をつけ、指したところの時刻と値を出す。
/// </summary>
/// <remarks>
/// 色はすべて依存関係プロパティで受け取り、既定のスタイル（<c>Themes/YamoriTheme.xaml</c>）でテーマの色（ライト・ダーク・ハイコントラスト）を渡す。
/// テーマが変わると色が変わり、描き直す。外部のライブラリは使わない。
/// </remarks>
public sealed partial class TimeSeriesChart : UserControl
{
    /// <summary><see cref="Data"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(object), typeof(TimeSeriesChart), new PropertyMetadata(null, OnVisualChanged));

    /// <summary><see cref="Series1Brush"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty Series1BrushProperty = RegisterBrush(nameof(Series1Brush));

    /// <summary><see cref="Series2Brush"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty Series2BrushProperty = RegisterBrush(nameof(Series2Brush));

    /// <summary><see cref="Series3Brush"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty Series3BrushProperty = RegisterBrush(nameof(Series3Brush));

    /// <summary><see cref="BandBrush"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty BandBrushProperty = RegisterBrush(nameof(BandBrush));

    /// <summary><see cref="CriticalBandBrush"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty CriticalBandBrushProperty = RegisterBrush(nameof(CriticalBandBrush));

    /// <summary><see cref="BandStrokeBrush"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty BandStrokeBrushProperty = RegisterBrush(nameof(BandStrokeBrush));

    /// <summary><see cref="GridBrush"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty GridBrushProperty = RegisterBrush(nameof(GridBrush));

    /// <summary><see cref="AxisTextBrush"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty AxisTextBrushProperty = RegisterBrush(nameof(AxisTextBrush));

    /// <summary><see cref="TipBackgroundBrush"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty TipBackgroundBrushProperty = RegisterBrush(nameof(TipBackgroundBrush));

    /// <summary><see cref="TipBorderBrush"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty TipBorderBrushProperty = RegisterBrush(nameof(TipBorderBrush));

    private const double LeftMargin = 52;
    private const double RightMargin = 52;
    private const double TopMargin = 8;
    private const double BottomMargin = 26;
    private const double LabelFontSize = 11;
    private const double LineThickness = 1.6;
    private const double EnvelopeOpacity = 0.16;
    private const double MinimumBandWidth = 3;
    private const double PixelsPerTimeTick = 110;

    private readonly Canvas _plot = new() { Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
    private readonly StackPanel _legend = new() { Orientation = Orientation.Horizontal, Spacing = 16, Margin = new Thickness(LeftMargin, 0, 0, 8) };
    private readonly Line _crosshair = new() { StrokeThickness = 1, Visibility = Visibility.Collapsed, IsHitTestVisible = false };
    private readonly TextBlock _tipText = new() { FontSize = 12, TextWrapping = TextWrapping.NoWrap };
    private readonly Border _tip;

    /// <summary>
    /// グラフを作る。
    /// </summary>
    public TimeSeriesChart()
    {
        _tip = new Border
        {
            Child = _tipText,
            Padding = new Thickness(10, 6, 10, 6),
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var plotHost = new Grid();
        plotHost.Children.Add(_plot);
        plotHost.Children.Add(_tip);
        Grid.SetRow(plotHost, 1);
        root.Children.Add(_legend);
        root.Children.Add(plotHost);
        Content = root;

        _plot.SizeChanged += (_, _) => Redraw();
        _plot.PointerMoved += OnPointerMoved;
        _plot.PointerExited += OnPointerExited;
        _plot.PointerCanceled += OnPointerExited;
    }

    /// <summary>描く内容（<see cref="ChartData"/>）を取得または設定する。null なら何も描かない。</summary>
    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    /// <summary>1 本目の系列の色を取得または設定する。</summary>
    public Brush? Series1Brush
    {
        get => (Brush?)GetValue(Series1BrushProperty);
        set => SetValue(Series1BrushProperty, value);
    }

    /// <summary>2 本目の系列の色を取得または設定する。</summary>
    public Brush? Series2Brush
    {
        get => (Brush?)GetValue(Series2BrushProperty);
        set => SetValue(Series2BrushProperty, value);
    }

    /// <summary>3 本目の系列の色を取得または設定する。</summary>
    public Brush? Series3Brush
    {
        get => (Brush?)GetValue(Series3BrushProperty);
        set => SetValue(Series3BrushProperty, value);
    }

    /// <summary>異常の区間の塗りの色を取得または設定する。</summary>
    public Brush? BandBrush
    {
        get => (Brush?)GetValue(BandBrushProperty);
        set => SetValue(BandBrushProperty, value);
    }

    /// <summary>重さが「危険」の異常の区間の塗りの色を取得または設定する。</summary>
    public Brush? CriticalBandBrush
    {
        get => (Brush?)GetValue(CriticalBandBrushProperty);
        set => SetValue(CriticalBandBrushProperty, value);
    }

    /// <summary>異常の区間の枠の色を取得または設定する（選んでいる区間は太く描く。ハイコントラストでも区間が分かるように、枠は常に描く）。</summary>
    public Brush? BandStrokeBrush
    {
        get => (Brush?)GetValue(BandStrokeBrushProperty);
        set => SetValue(BandStrokeBrushProperty, value);
    }

    /// <summary>補助線の色を取得または設定する。</summary>
    public Brush? GridBrush
    {
        get => (Brush?)GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    /// <summary>軸の文字の色を取得または設定する。</summary>
    public Brush? AxisTextBrush
    {
        get => (Brush?)GetValue(AxisTextBrushProperty);
        set => SetValue(AxisTextBrushProperty, value);
    }

    /// <summary>指したところの値の吹き出しの背景を取得または設定する。</summary>
    public Brush? TipBackgroundBrush
    {
        get => (Brush?)GetValue(TipBackgroundBrushProperty);
        set => SetValue(TipBackgroundBrushProperty, value);
    }

    /// <summary>指したところの値の吹き出しの枠の色を取得または設定する。</summary>
    public Brush? TipBorderBrush
    {
        get => (Brush?)GetValue(TipBorderBrushProperty);
        set => SetValue(TipBorderBrushProperty, value);
    }

    private ChartData? Chart => Data as ChartData;

    private static DependencyProperty RegisterBrush(string name) =>
        DependencyProperty.Register(name, typeof(Brush), typeof(TimeSeriesChart), new PropertyMetadata(null, OnVisualChanged));

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TimeSeriesChart)d).Redraw();

    private Brush? SeriesBrush(int index) => (index % 3) switch
    {
        0 => Series1Brush,
        1 => Series2Brush,
        _ => Series3Brush,
    };

    private void Redraw()
    {
        _plot.Children.Clear();
        _tip.Visibility = Visibility.Collapsed;
        _tip.Background = TipBackgroundBrush;
        _tip.BorderBrush = TipBorderBrush;
        _tipText.Foreground = AxisTextBrush;
        _crosshair.Stroke = AxisTextBrush;
        _crosshair.Visibility = Visibility.Collapsed;
        DrawLegend();

        var chart = Chart;
        var width = _plot.ActualWidth;
        var height = _plot.ActualHeight;
        if (chart is null || chart.Range.Length <= TimeSpan.Zero || width <= LeftMargin + RightMargin || height <= TopMargin + BottomMargin)
        {
            return;
        }

        var area = new Rect(LeftMargin, TopMargin, width - LeftMargin - RightMargin, height - TopMargin - BottomMargin);
        DrawBands(chart, area);
        DrawValueAxes(chart, area);
        DrawTimeAxis(chart, area);
        DrawReferenceLine(chart, area);
        for (var i = 0; i < chart.Series.Count; i++)
        {
            DrawSeries(chart.Series[i], chart.Range, area);
        }

        _plot.Children.Add(_crosshair);
    }

    private void DrawLegend()
    {
        _legend.Children.Clear();
        if (Chart is not { } chart)
        {
            return;
        }

        foreach (var series in chart.Series)
        {
            var item = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            item.Children.Add(new Rectangle
            {
                Width = 14,
                Height = 3,
                RadiusX = 1.5,
                RadiusY = 1.5,
                VerticalAlignment = VerticalAlignment.Center,
                Fill = SeriesBrush(series.ColorIndex),
            });
            item.Children.Add(new TextBlock { Text = series.Name, FontSize = 12, Foreground = AxisTextBrush });
            _legend.Children.Add(item);
        }
    }

    private void DrawBands(ChartData chart, Rect area)
    {
        foreach (var band in chart.Bands)
        {
            var left = Math.Clamp(X(band.StartUtc, chart.Range, area), area.Left, area.Right);
            var right = Math.Clamp(X(band.EndUtc, chart.Range, area), area.Left, area.Right);
            var bandWidth = Math.Max(MinimumBandWidth, right - left);
            var rectangle = new Rectangle
            {
                Width = bandWidth,
                Height = area.Height,
                Fill = band.IsCritical ? CriticalBandBrush : BandBrush,
                Stroke = BandStrokeBrush,
                StrokeThickness = band.IsSelected ? 2 : 0.5,
                IsHitTestVisible = false,
            };
            Canvas.SetLeft(rectangle, Math.Min(left, area.Right - bandWidth));
            Canvas.SetTop(rectangle, area.Top);
            _plot.Children.Add(rectangle);
        }
    }

    private void DrawValueAxes(ChartData chart, Rect area)
    {
        var left = chart.Series.FirstOrDefault(s => s.AxisSide == ChartAxisSide.Left);
        var right = chart.Series.FirstOrDefault(s => s.AxisSide == ChartAxisSide.Right);
        var gridSeries = left ?? right;
        if (gridSeries is not null)
        {
            foreach (var tick in gridSeries.Scale.Ticks())
            {
                var y = Y(tick, gridSeries.Scale, area);
                _plot.Children.Add(new Line { X1 = area.Left, X2 = area.Right, Y1 = y, Y2 = y, Stroke = GridBrush, StrokeThickness = 1, IsHitTestVisible = false });
            }
        }

        if (left is not null)
        {
            DrawAxisLabels(left, area, isLeft: true);
        }

        if (right is not null)
        {
            DrawAxisLabels(right, area, isLeft: false);
        }
    }

    private void DrawAxisLabels(ChartSeries series, Rect area, bool isLeft)
    {
        foreach (var tick in series.Scale.Ticks())
        {
            var label = AxisLabel(series.FormatValue(tick), isLeft ? TextAlignment.Right : TextAlignment.Left, SeriesBrush(series.ColorIndex));
            label.Width = LeftMargin - 8;
            Canvas.SetLeft(label, isLeft ? 0 : area.Right + 8);
            Canvas.SetTop(label, Y(tick, series.Scale, area) - (LabelFontSize * 0.8));
            _plot.Children.Add(label);
        }
    }

    private void DrawTimeAxis(ChartData chart, Rect area)
    {
        var maxTicks = Math.Max(2, (int)(area.Width / PixelsPerTimeTick));
        var step = TimeAxisTicks.ChooseStep(chart.Range, maxTicks);
        var culture = CultureInfo.CurrentUICulture;
        foreach (var tick in TimeAxisTicks.Create(chart.Range, TimeZoneInfo.Local, maxTicks))
        {
            var x = X(tick, chart.Range, area);
            _plot.Children.Add(new Line { X1 = x, X2 = x, Y1 = area.Bottom, Y2 = area.Bottom + 4, Stroke = GridBrush, StrokeThickness = 1, IsHitTestVisible = false });
            var label = AxisLabel(ChartTimeLabelFormatter.Tick(tick.ToLocalTime(), step, culture), TextAlignment.Center, AxisTextBrush);
            label.Width = PixelsPerTimeTick;
            Canvas.SetLeft(label, x - (PixelsPerTimeTick / 2));
            Canvas.SetTop(label, area.Bottom + 6);
            _plot.Children.Add(label);
        }

        _plot.Children.Add(new Line { X1 = area.Left, X2 = area.Right, Y1 = area.Bottom, Y2 = area.Bottom, Stroke = GridBrush, StrokeThickness = 1, IsHitTestVisible = false });
    }

    private void DrawReferenceLine(ChartData chart, Rect area)
    {
        if (chart.ReferenceLine is not { } reference || reference.SeriesIndex < 0 || reference.SeriesIndex >= chart.Series.Count)
        {
            return;
        }

        var series = chart.Series[reference.SeriesIndex];
        var y = Y(reference.Value, series.Scale, area);
        var brush = SeriesBrush(series.ColorIndex);
        _plot.Children.Add(new Line
        {
            X1 = area.Left,
            X2 = area.Right,
            Y1 = y,
            Y2 = y,
            Stroke = brush,
            StrokeThickness = 1,
            StrokeDashArray = [4, 3],
            IsHitTestVisible = false,
        });
        var label = AxisLabel(reference.Label, TextAlignment.Right, brush);
        label.Width = area.Width - 8;
        Canvas.SetLeft(label, area.Left);
        Canvas.SetTop(label, y - (LabelFontSize * 1.7));
        _plot.Children.Add(label);
    }

    private void DrawSeries(ChartSeries series, AnalysisRange range, Rect area)
    {
        var brush = SeriesBrush(series.ColorIndex);
        var line = new PathGeometry();
        var envelope = new PathGeometry();
        foreach (var segment in Segments(series))
        {
            var points = segment.Select(p => new Point(X(p.TimestampUtc, range, area), Y(p.Average, series.Scale, area))).ToArray();
            var lineFigure = new PathFigure { StartPoint = points[0], IsClosed = false, IsFilled = false };
            var polyline = new PolyLineSegment();
            foreach (var point in points.Skip(1).DefaultIfEmpty(new Point(points[0].X + 1, points[0].Y)))
            {
                polyline.Points.Add(point);
            }

            lineFigure.Segments.Add(polyline);
            line.Figures.Add(lineFigure);

            var high = segment.Select(p => new Point(X(p.TimestampUtc, range, area), Y(p.Max, series.Scale, area)));
            var low = segment.Select(p => new Point(X(p.TimestampUtc, range, area), Y(p.Min, series.Scale, area))).Reverse();
            var outline = high.Concat(low).ToArray();
            var envelopeFigure = new PathFigure { StartPoint = outline[0], IsClosed = true, IsFilled = true };
            var polygon = new PolyLineSegment();
            foreach (var point in outline.Skip(1))
            {
                polygon.Points.Add(point);
            }

            envelopeFigure.Segments.Add(polygon);
            envelope.Figures.Add(envelopeFigure);
        }

        _plot.Children.Add(new Path { Data = envelope, Fill = brush, Opacity = EnvelopeOpacity, IsHitTestVisible = false });
        _plot.Children.Add(new Path
        {
            Data = line,
            Stroke = brush,
            StrokeThickness = LineThickness,
            StrokeLineJoin = PenLineJoin.Round,
            IsHitTestVisible = false,
        });
    }

    /// <summary>
    /// 点を、記録が途切れたところ（間が <see cref="ChartSeries.MaxGap"/> より空いたところ）で区切る。
    /// </summary>
    private static IEnumerable<IReadOnlyList<SensorHistoryPoint>> Segments(ChartSeries series)
    {
        var current = new List<SensorHistoryPoint>();
        foreach (var point in series.Points.Where(p => double.IsFinite(p.Average)))
        {
            if (current.Count > 0 && point.TimestampUtc - current[^1].TimestampUtc > series.MaxGap)
            {
                yield return current;
                current = [];
            }

            current.Add(point);
        }

        if (current.Count > 0)
        {
            yield return current;
        }
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var chart = Chart;
        var width = _plot.ActualWidth;
        var height = _plot.ActualHeight;
        if (chart is null || chart.Series.Count == 0 || width <= LeftMargin + RightMargin || height <= TopMargin + BottomMargin)
        {
            return;
        }

        var area = new Rect(LeftMargin, TopMargin, width - LeftMargin - RightMargin, height - TopMargin - BottomMargin);
        var position = e.GetCurrentPoint(_plot).Position;
        if (position.X < area.Left || position.X > area.Right)
        {
            HideTip();
            return;
        }

        var time = chart.Range.FromUtc + TimeSpan.FromTicks((long)((position.X - area.Left) / area.Width * chart.Range.Length.Ticks));
        var lines = new List<string>();
        var fine = false;
        foreach (var series in chart.Series)
        {
            if (Nearest(series, time) is { } point)
            {
                lines.Add($"{series.Name}  {series.FormatValue(point.Average)}");
                fine |= series.MaxGap < TimeSpan.FromMinutes(1);
            }
        }

        if (lines.Count == 0)
        {
            HideTip();
            return;
        }

        lines.Insert(0, ChartTimeLabelFormatter.Point(time.ToLocalTime(), fine, CultureInfo.CurrentUICulture));
        _tipText.Text = string.Join(Environment.NewLine, lines);
        _crosshair.X1 = _crosshair.X2 = position.X;
        _crosshair.Y1 = area.Top;
        _crosshair.Y2 = area.Bottom;
        _crosshair.Visibility = Visibility.Visible;

        _tip.Visibility = Visibility.Visible;
        _tip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var tipLeft = position.X + 12 + _tip.DesiredSize.Width > area.Right ? position.X - 12 - _tip.DesiredSize.Width : position.X + 12;
        _tip.Margin = new Thickness(Math.Max(0, tipLeft), area.Top + 4, 0, 0);
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e) => HideTip();

    private void HideTip()
    {
        _tip.Visibility = Visibility.Collapsed;
        _crosshair.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// 時刻にいちばん近い点を探す。近くに点がなければ（記録が途切れている）null。
    /// </summary>
    private static SensorHistoryPoint? Nearest(ChartSeries series, DateTimeOffset time)
    {
        var points = series.Points;
        if (points.Count == 0)
        {
            return null;
        }

        int low = 0, high = points.Count - 1;
        while (low < high)
        {
            var middle = (low + high) / 2;
            if (points[middle].TimestampUtc < time)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        var best = points[low];
        if (low > 0 && (time - points[low - 1].TimestampUtc).Duration() < (best.TimestampUtc - time).Duration())
        {
            best = points[low - 1];
        }

        return (best.TimestampUtc - time).Duration() <= series.MaxGap ? best : null;
    }

    private TextBlock AxisLabel(string text, TextAlignment alignment, Brush? foreground) =>
        new()
        {
            Text = text,
            FontSize = LabelFontSize,
            TextAlignment = alignment,
            Foreground = foreground ?? AxisTextBrush,
            TextTrimming = TextTrimming.CharacterEllipsis,
            IsHitTestVisible = false,
        };

    private static double X(DateTimeOffset time, AnalysisRange range, Rect area) =>
        area.Left + ((time - range.FromUtc).Ticks / (double)range.Length.Ticks * area.Width);

    private static double Y(double value, ChartAxisScale scale, Rect area)
    {
        var span = scale.Maximum - scale.Minimum;
        var ratio = span > 0 ? (Math.Clamp(value, scale.Minimum, scale.Maximum) - scale.Minimum) / span : 0.5;
        return area.Top + ((1 - ratio) * area.Height);
    }
}
