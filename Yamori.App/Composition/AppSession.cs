using System;
using System.Threading;
using System.Threading.Tasks;
using Yamori.ViewModels;
using Yamori.Views;

namespace Yamori.Composition;

/// <summary>
/// 起動からサインイン、メイン画面の表示、言語を変えたときの画面の作り直しまでの流れを受け持つ。
/// </summary>
public sealed class AppSession : IDisposable
{
    private readonly AppServices _services;
    private readonly MainWindow _window;
    private readonly CancellationTokenSource _monitorCancellation = new();
    private MainViewModel? _main;

    /// <summary>
    /// 部品とウィンドウを指定して作る。
    /// </summary>
    /// <param name="services">組み立て済みの部品。</param>
    /// <param name="window">表示先のウィンドウ。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public AppSession(AppServices services, MainWindow window)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(window);
        _services = services;
        _window = window;
    }

    /// <summary>
    /// サインイン画面を表示する。サインインが済むとメイン画面に切り替わる。
    /// </summary>
    public void Start()
    {
        var login = _services.CreateLoginViewModel(() => _window.Handle);
        login.SignedIn += OnSignedIn;
        _window.ShowContent(new LoginView(login));
        _ = login.InitializeAsync();    }

    /// <summary>
    /// 使用率の記録を止め、イベントの購読を解除する。
    /// </summary>
    public void Dispose()
    {
        _monitorCancellation.Cancel();
        _monitorCancellation.Dispose();
        _main?.Dispose();
    }

    private void OnSignedIn(object? sender, EventArgs e)
    {
        if (sender is LoginViewModel login)
        {
            login.SignedIn -= OnSignedIn;
        }

        // 記録はサインインした後に始める。
        var token = _monitorCancellation.Token;
        _ = Task.Run(() => _services.Monitor.RunAsync(token), token);

        _main = _services.CreateMainViewModel();
        ShowShell(openSettings: false);
        _ = _main.RefreshCommand.ExecuteAsync(null);
    }

    private void ShowShell(bool openSettings)
    {
        var settings = _services.CreateSettingsViewModel(() => _window.Id);
        settings.LanguageChanged += OnLanguageChanged;
        _window.ShowContent(new ShellView(_main!, settings, openSettings));
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (sender is SettingsViewModel settings)
        {
            settings.LanguageChanged -= OnLanguageChanged;
        }

        // 文字列は画面を作った時点の言語で決まるため、画面ごと作り直す。
        ShowShell(openSettings: true);
        _ = _main!.ReloadForLanguageAsync();
    }
}
