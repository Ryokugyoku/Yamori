using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Yamori.Controls;

/// <summary>
/// 割合（0〜100）を円の弧で示すグラフ。真上から時計回りに伸びる。
/// </summary>
public sealed partial class RingGauge : UserControl
{
    /// <summary><see cref="Value"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(RingGauge), new PropertyMetadata(0d, OnShapeChanged));

    /// <summary><see cref="RingThickness"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty RingThicknessProperty =
        DependencyProperty.Register(nameof(RingThickness), typeof(double), typeof(RingGauge), new PropertyMetadata(6d, OnShapeChanged));

    /// <summary><see cref="TrackBrush"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty TrackBrushProperty =
        DependencyProperty.Register(nameof(TrackBrush), typeof(Brush), typeof(RingGauge), new PropertyMetadata(null, OnBrushChanged));

    /// <summary><see cref="ValueBrush"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty ValueBrushProperty =
        DependencyProperty.Register(nameof(ValueBrush), typeof(Brush), typeof(RingGauge), new PropertyMetadata(null, OnBrushChanged));

    /// <summary><see cref="CenterText"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty CenterTextProperty =
        DependencyProperty.Register(nameof(CenterText), typeof(string), typeof(RingGauge), new PropertyMetadata(string.Empty, OnCenterTextChanged));

    private readonly Ellipse _track = new() { Opacity = 0.5 };
    private readonly Path _arc = new() { StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
    private readonly TextBlock _center = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        FontSize = 12,
    };

    /// <summary>
    /// グラフを作る。
    /// </summary>
    public RingGauge()
    {
        var root = new Grid();
        root.Children.Add(_track);
        root.Children.Add(_arc);
        root.Children.Add(_center);
        Content = root;
        SizeChanged += (_, _) => Redraw();
    }

    /// <summary>示す割合（0〜100）を取得または設定する。範囲の外は丸める。</summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>円の線の太さ（ピクセル）を取得または設定する。</summary>
    public double RingThickness
    {
        get => (double)GetValue(RingThicknessProperty);
        set => SetValue(RingThicknessProperty, value);
    }

    /// <summary>円の下地の色を取得または設定する。</summary>
    public Brush? TrackBrush
    {
        get => (Brush?)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    /// <summary>割合を示す弧の色を取得または設定する。</summary>
    public Brush? ValueBrush
    {
        get => (Brush?)GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    /// <summary>円の中央に出す文字列を取得または設定する。</summary>
    public string CenterText
    {
        get => (string)GetValue(CenterTextProperty);
        set => SetValue(CenterTextProperty, value);
    }

    private static void OnShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((RingGauge)d).Redraw();

    private static void OnBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var gauge = (RingGauge)d;
        gauge._track.Stroke = gauge.TrackBrush;
        gauge._arc.Stroke = gauge.ValueBrush;
    }

    private static void OnCenterTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((RingGauge)d)._center.Text = e.NewValue as string ?? string.Empty;

    private void Redraw()
    {
        var size = Math.Min(ActualWidth, ActualHeight);
        var thickness = RingThickness;
        _track.StrokeThickness = thickness;
        _arc.StrokeThickness = thickness;
        _track.Width = size;
        _track.Height = size;

        var ratio = Math.Clamp(Value, 0, 100) / 100;
        if (size <= thickness || ratio <= 0)
        {
            _arc.Data = null;
            return;
        }

        var radius = (size - thickness) / 2;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);

        // 弧は 1 周の手前で止める（始点と終点が同じだと描けないため）。
        var angle = Math.Min(ratio, 0.9999) * 2 * Math.PI;
        var start = new Point(center.X, center.Y - radius);
        var end = new Point(center.X + (radius * Math.Sin(angle)), center.Y - (radius * Math.Cos(angle)));

        var figure = new PathFigure { StartPoint = start, IsClosed = false, IsFilled = false };
        figure.Segments.Add(new ArcSegment
        {
            Point = end,
            Size = new Size(radius, radius),
            IsLargeArc = angle > Math.PI,
            SweepDirection = SweepDirection.Clockwise,
        });
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        _arc.Data = geometry;
    }
}
