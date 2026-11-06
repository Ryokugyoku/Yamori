using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Yamori.Views;

/// <summary>
/// 各画面の上部に置く見出し。タイトル、説明、読み込み中の表示、更新ボタンを持つ。
/// </summary>
public sealed partial class PageHeader : UserControl
{
    /// <summary><see cref="Title"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(PageHeader), new PropertyMetadata(string.Empty));

    /// <summary><see cref="Description"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(PageHeader), new PropertyMetadata(string.Empty));

    /// <summary><see cref="IsBusy"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty IsBusyProperty =
        DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(PageHeader), new PropertyMetadata(false));

    /// <summary><see cref="RefreshCommand"/> の依存関係プロパティ。</summary>
    public static readonly DependencyProperty RefreshCommandProperty =
        DependencyProperty.Register(nameof(RefreshCommand), typeof(ICommand), typeof(PageHeader), new PropertyMetadata(null, OnRefreshCommandChanged));

    /// <summary>
    /// 見出しを作る。
    /// </summary>
    public PageHeader()
    {
        InitializeComponent();
    }

    /// <summary>タイトルを取得または設定する。</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>タイトルの下の説明を取得または設定する。</summary>
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>読み込み中の表示を出すかどうかを取得または設定する。</summary>
    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    /// <summary>更新ボタンのコマンドを取得または設定する。null なら更新ボタンを出さない。</summary>
    public ICommand? RefreshCommand
    {
        get => (ICommand?)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }

    /// <summary>更新ボタンを表示するかどうかを取得する。</summary>
    public Visibility ShowsRefresh => RefreshCommand is null ? Visibility.Collapsed : Visibility.Visible;

    private static void OnRefreshCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((PageHeader)d).Bindings.Update();
}
