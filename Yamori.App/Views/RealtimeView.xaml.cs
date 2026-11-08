using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yamori.Localization;
using Yamori.Services;
using Yamori.ViewModels;
using Yamori.ViewModels.Sensors;

namespace Yamori.Views;

/// <summary>
/// リアルタイム画面。左の一覧でハードウェアかアプリを選び、右にその詳細（主な値、グループごとのすべての値、アプリごとの今の使用量）を表示する。
/// アプリごとの使用量は、画面が表示されている間だけ測定し、閉じると止める。
/// </summary>
public sealed partial class RealtimeView : UserControl, ISelfScrollingPage
{
    /// <summary>
    /// 表示するビューモデルを指定して作る。ビューモデルは、この画面が閉じられたときに破棄する。
    /// </summary>
    /// <param name="viewModel">画面の状態。</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewModel"/> が null。</exception>
    public RealtimeView(RealtimePageViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>画面の状態を取得する。x:Bind の参照先。</summary>
    public RealtimePageViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ShowSelection();
        ViewModel.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
    }

    /// <summary>
    /// ビューモデルが選んだもの（最初の 1 つや、消えたハードウェアの代わり）を、左の一覧の選択に映す。
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RealtimePageViewModel.SelectedHardware) or nameof(RealtimePageViewModel.IsAppsSelected))
        {
            ShowSelection();
        }
    }

    private void ShowSelection()
    {
        if (ViewModel.IsAppsSelected)
        {
            HardwareList.SelectedItem = null;
            AppsList.SelectedIndex = 0;
            return;
        }

        AppsList.SelectedItem = null;
        if (!ReferenceEquals(HardwareList.SelectedItem, ViewModel.SelectedHardware))
        {
            HardwareList.SelectedItem = ViewModel.SelectedHardware;
        }
    }

    private void OnHardwareSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HardwareList.SelectedItem is SensorHardwareItem hardware)
        {
            ViewModel.SelectHardware(hardware);
        }
    }

    private void OnAppsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppsList.SelectedIndex == 0)
        {
            ViewModel.SelectApps();
        }
    }

    /// <summary>
    /// お知らせのボタンが押されたら、Helper が動いていなければサービスを起動し、そうでなければ PawnIO の利用の同意を変える。
    /// </summary>
    /// <remarks>
    /// ボタンの処理（async void）で例外が出るとアプリが落ちるため、ここで止めて書き残す。
    /// ダイアログが別に開いている、待っている間に画面が閉じられた、などで起こりうる。
    /// </remarks>
    private async void OnNoticeActionClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel.Tree.CanStartHelper)
            {
                await StartHelperAsync();
            }
            else
            {
                await ChangeConsentAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Write(nameof(RealtimeView), ex);
        }
    }

    /// <summary>
    /// Helper のサービスを起動する。起動できなければ、その理由を出す。
    /// </summary>
    private async Task StartHelperAsync()
    {
        var error = await ViewModel.Tree.StartHelperAsync();
        if (error is not null)
        {
            await ShowErrorAsync(UiText.Get("HelperStart_ErrorTitle"), error);
        }
    }

    /// <summary>
    /// 失敗の理由をダイアログで出す。
    /// </summary>
    /// <param name="title">見出し。</param>
    /// <param name="message">理由の説明。</param>
    private async Task ShowErrorAsync(string title, string message)
    {
        if (XamlRoot is null)
        {
            // 待っている間に画面が閉じられた。
            return;
        }

        await new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = title,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            CloseButtonText = UiText.Get("Consent_ErrorClose"),
        }.ShowAsync();
    }

    /// <summary>
    /// PawnIO の利用の同意の画面を出し、確かめてから同意を変える。
    /// </summary>
    private async Task ChangeConsentAsync()
    {
        if (ViewModel.Tree.ConsentAction is not { } enable)
        {
            return;
        }

        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = UiText.Get("Consent_DialogTitle"),
            Content = new TextBlock { Text = UiText.Get(enable ? "Consent_DialogBody" : "Consent_DialogDisableBody"), TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = UiText.Get(enable ? "Consent_DialogConfirmEnable" : "Consent_DialogConfirmDisable"),
            CloseButtonText = UiText.Get("Consent_DialogCancel"),
            DefaultButton = ContentDialogButton.Close,
        };
        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        var error = await ViewModel.Tree.ApplyConsentAsync(enable);
        if (error is null)
        {
            return;
        }

        await ShowErrorAsync(UiText.Get("Consent_ErrorTitle"), error);
    }
}
