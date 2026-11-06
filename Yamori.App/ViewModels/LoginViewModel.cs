using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Yamori.Accounts;
using Yamori.Localization;

namespace Yamori.ViewModels;

/// <summary>
/// サインイン画面の状態。Windows アカウントの本人確認を Windows Hello で行う。
/// </summary>
/// <remarks>
/// Windows Hello が設定されていない PC では、すでに Windows にサインインしているアカウントのまま続行できる。
/// </remarks>
public sealed class LoginViewModel : ObservableObject
{
    private readonly IAccountVerifier _verifier;

    private bool _isHelloAvailable = true;
    private bool _isBusy = true;
    private string _statusText;

    /// <summary>
    /// 必要な処理を指定して作る。
    /// </summary>
    /// <param name="accountProvider">サインイン中のアカウントを取得する処理。</param>
    /// <param name="verifier">本人確認の処理。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public LoginViewModel(IWindowsAccountProvider accountProvider, IAccountVerifier verifier)
    {
        ArgumentNullException.ThrowIfNull(accountProvider);
        ArgumentNullException.ThrowIfNull(verifier);

        _verifier = verifier;
        Account = accountProvider.GetCurrent();
        _statusText = UiText.Get("Login_Checking");
        SignInCommand = new AsyncRelayCommand(SignInAsync, () => !IsBusy);
    }

    /// <summary>
    /// 本人確認が済み、アプリを使い始めてよくなったときに発生する。
    /// </summary>
    public event EventHandler? SignedIn;

    /// <summary>サインインするアカウントを取得する。</summary>
    public WindowsAccount Account { get; }

    /// <summary>サインイン（または続行）のコマンドを取得する。</summary>
    public IAsyncRelayCommand SignInCommand { get; }

    /// <summary>Windows Hello が使えるかどうかを取得する。</summary>
    public bool IsHelloAvailable
    {
        get => _isHelloAvailable;
        private set
        {
            if (SetProperty(ref _isHelloAvailable, value))
            {
                OnPropertyChanged(nameof(SignInButtonText));
            }
        }
    }

    /// <summary>サインインボタンの文字列を取得する。</summary>
    public string SignInButtonText => UiText.Get(IsHelloAvailable ? "Login_SignInButton" : "Login_ContinueButton");

    /// <summary>確認中かどうかを取得する。</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                SignInCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>状態の説明を取得する。空なら何も表示しない。</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Windows Hello が使えるかを調べ、画面の表示を整える。画面を表示した直後に呼ぶ。
    /// </summary>
    /// <returns>調べ終わったときに完了するタスク。</returns>
    public async Task InitializeAsync()
    {
        IsHelloAvailable = await _verifier.IsAvailableAsync();
        StatusText = IsHelloAvailable ? string.Empty : UiText.Get("Login_HelloUnavailable");
        IsBusy = false;
    }

    private async Task SignInAsync()
    {
        if (!IsHelloAvailable)
        {
            SignedIn?.Invoke(this, EventArgs.Empty);
            return;
        }

        IsBusy = true;
        StatusText = UiText.Get("Login_Verifying");
        try
        {
            var result = await _verifier.VerifyAsync(UiText.Get("Login_HelloPrompt"));
            switch (result)
            {
                case AccountVerificationResult.Verified:
                    StatusText = string.Empty;
                    SignedIn?.Invoke(this, EventArgs.Empty);
                    break;
                case AccountVerificationResult.Unavailable:
                    IsHelloAvailable = false;
                    StatusText = UiText.Get("Login_HelloUnavailable");
                    break;
                case AccountVerificationResult.Canceled:
                    StatusText = UiText.Get("Login_Canceled");
                    break;
                default:
                    StatusText = UiText.Get("Login_Failed");
                    break;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
