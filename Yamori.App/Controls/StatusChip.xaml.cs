using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yamori.Presentation;

namespace Yamori.Controls;

/// <summary>
/// 状態を色つきの点と短い文字で示す札（例：「見守り中」「余裕あり」）。色はテーマに合わせて切り替わる。
/// </summary>
public sealed partial class StatusChip : UserControl
{
    /// <summary><see cref="Level"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(nameof(Level), typeof(StatusLevel), typeof(StatusChip), new PropertyMetadata(StatusLevel.Neutral, OnLevelChanged));

    /// <summary><see cref="Text"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(StatusChip), new PropertyMetadata(string.Empty, OnTextChanged));

    /// <summary>
    /// 札を作る。
    /// </summary>
    public StatusChip()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyLevel(useTransitions: false);
    }

    /// <summary>色分けを取得または設定する。</summary>
    public StatusLevel Level
    {
        get => (StatusLevel)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    /// <summary>札の文字を取得または設定する。</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((StatusChip)d).ApplyLevel(useTransitions: true);

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((StatusChip)d).Label.Text = e.NewValue as string ?? string.Empty;

    private void ApplyLevel(bool useTransitions) => VisualStateManager.GoToState(this, Level.ToString(), useTransitions);
}
