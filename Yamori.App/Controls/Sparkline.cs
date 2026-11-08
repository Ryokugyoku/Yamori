using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Yamori.Controls;

/// <summary>
/// 直近の値の動きを示す、小さな折れ線グラフ。新しい値ほど右に描き、値が少ないうちは右に寄せて描く。
/// </summary>
public sealed partial class Sparkline : UserControl
{
    /// <summary><see cref="Values"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty ValuesProperty =
        DependencyProperty.Register(nameof(Values), typeof(object), typeof(Sparkline), new PropertyMetadata(null, OnShapeChanged));

    /// <summary><see cref="Minimum"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(Sparkline), new PropertyMetadata(double.NaN, OnShapeChanged));

    /// <summary><see cref="Maximum"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(Sparkline), new PropertyMetadata(double.NaN, OnShapeChanged));

    /// <summary><see cref="Capacity"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty CapacityProperty =
        DependencyProperty.Register(nameof(Capacity), typeof(int), typeof(Sparkline), new PropertyMetadata(60, OnShapeChanged));

    /// <summary><see cref="Stroke"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(Sparkline), new PropertyMetadata(null, OnStrokeChanged));

    /// <summary>線の太さ（ピクセル）。</summary>
    private const double LineThickness = 2;

    /// <summary>最後の値に置く点の直径（ピクセル）。</summary>
    private const double DotSize = 6;

    /// <summary>線の下を塗る色の不透明度。</summary>
    private const double AreaOpacity = 0.14;

    private readonly Path _area = new() { Opacity = AreaOpacity };
    private readonly Path _line = new() { StrokeThickness = LineThickness, StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
    private readonly Ellipse _dot = new() { Width = DotSize, Height = DotSize, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };

    /// <summary>
    /// グラフを作る。
    /// </summary>
    public Sparkline()
    {
        var root = new Grid();
        root.Children.Add(_area);
        root.Children.Add(_line);
        root.Children.Add(_dot);
        Content = root;
        SizeChanged += (_, _) => Redraw();
    }

    /// <summary>描く値（古い順）を取得または設定する。<see cref="IReadOnlyList{T}"/>（double）を渡す。</summary>
    public object? Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>縦軸の下限を取得または設定する。NaN なら値から決める。</summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>縦軸の上限を取得または設定する。NaN なら値から決める。</summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>横幅いっぱいに並べる値の数を取得または設定する。</summary>
    public int Capacity
    {
        get => (int)GetValue(CapacityProperty);
        set => SetValue(CapacityProperty, value);
    }

    /// <summary>線の色を取得または設定する。線の下の塗りにも、薄くして使う。</summary>
    public Brush? Stroke
    {
        get => (Brush?)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    private static void OnShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((Sparkline)d).Redraw();

    private static void OnStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var sparkline = (Sparkline)d;
        var brush = e.NewValue as Brush;
        sparkline._line.Stroke = brush;
        sparkline._area.Fill = brush;
        sparkline._dot.Fill = brush;
    }

    private void Redraw()
    {
        var values = Values as IReadOnlyList<double> ?? [];
        var width = ActualWidth;
        var height = ActualHeight;
        if (values.Count < 2 || width <= 0 || height <= 0)
        {
            _line.Data = null;
            _area.Data = null;
            _dot.Visibility = Visibility.Collapsed;
            return;
        }

        var (low, high) = Range(values);
        var top = DotSize / 2;
        var usable = Math.Max(1, height - DotSize);
        var step = width / Math.Max(1, Math.Max(Capacity, values.Count) - 1);
        var start = width - (step * (values.Count - 1));

        Point ToPoint(int index) =>
            new(start + (step * index), top + (usable * (1 - ((Math.Clamp(values[index], low, high) - low) / (high - low)))));

        var line = new PolyLineSegment();
        var area = new PolyLineSegment();
        for (var i = 1; i < values.Count; i++)
        {
            var point = ToPoint(i);
            line.Points.Add(point);
            area.Points.Add(point);
        }

        var first = ToPoint(0);
        var last = ToPoint(values.Count - 1);
        area.Points.Add(new Point(last.X, height));
        area.Points.Add(new Point(first.X, height));

        _line.Data = Geometry(first, line, isClosed: false);
        _area.Data = Geometry(first, area, isClosed: true);
        _dot.Margin = new Thickness(last.X - (DotSize / 2), last.Y - (DotSize / 2), 0, 0);
        _dot.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// 縦軸の範囲を決める。指定がなければ値の最小・最大に少し余白を足す。範囲の幅は 0 にしない。
    /// </summary>
    /// <remarks>
    /// 一覧の部品が別の項目に使い回されると、下限・上限・値が 1 つずつ書き換わり、途中で下限が上限を超えることがある
    /// （割合の 0〜100 から、GB の値に変わるときなど）。そのときは指定を使わず、値から決める。
    /// </remarks>
    private (double Low, double High) Range(IReadOnlyList<double> values)
    {
        var (low, high) = RangeFrom(values, Minimum, Maximum);
        if (low > high)
        {
            (low, high) = RangeFrom(values, double.NaN, double.NaN);
        }

        return high - low < 1e-9 ? (low - 1, high + 1) : (low, high);
    }

    /// <summary>
    /// 指定された下限・上限を使い、指定のない側（NaN）は値の最小・最大に少し余白を足して決める。
    /// </summary>
    private static (double Low, double High) RangeFrom(IReadOnlyList<double> values, double low, double high)
    {
        if (double.IsNaN(low) || double.IsNaN(high))
        {
            var min = double.MaxValue;
            var max = double.MinValue;
            foreach (var value in values)
            {
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }

            var margin = Math.Max((max - min) * 0.15, Math.Abs(max) * 0.02);
            low = double.IsNaN(low) ? min - margin : low;
            high = double.IsNaN(high) ? max + margin : high;
        }

        return (low, high);
    }

    private static PathGeometry Geometry(Point start, PolyLineSegment segment, bool isClosed)
    {
        var figure = new PathFigure { StartPoint = start, IsClosed = isClosed, IsFilled = isClosed };
        figure.Segments.Add(segment);
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }
}
