using Yamori.Core.Anomalies;
using Yamori.Core.Notifications;
using Yamori.Notifier.Windowing;

namespace Yamori.Notifier.Toasts;

/// <summary>
/// 異常の通知を、UI スレッド（隠しウィンドウのメッセージ ループ）で出す <see cref="IAnomalyToastSink"/>。
/// 一時停止の状態はトレイのメニューと同じ UI スレッドで読むため、出してよいかの判定もそこで行う。
/// </summary>
internal sealed class WindowToastSink : IAnomalyToastSink
{
    private readonly MessageWindow _window;
    private readonly NotificationGate _gate;
    private readonly AnomalyToastComposer _composer;
    private readonly ToastPresenter _presenter;

    /// <summary>
    /// 部品を指定して作る。
    /// </summary>
    /// <param name="window">処理を UI スレッドへ渡す先。</param>
    /// <param name="gate">一時停止中・全画面中かどうかの判定。</param>
    /// <param name="composer">通知の文面の作り方。</param>
    /// <param name="presenter">通知の出し方。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public WindowToastSink(MessageWindow window, NotificationGate gate, AnomalyToastComposer composer, ToastPresenter presenter)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(gate);
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(presenter);
        _window = window;
        _gate = gate;
        _composer = composer;
        _presenter = presenter;
    }

    /// <inheritdoc />
    /// <remarks>どのスレッドから呼んでもよい。UI スレッドで出し終えたら完了する。</remarks>
    public Task<bool> TryShowAsync(AnomalyRecord anomaly)
    {
        ArgumentNullException.ThrowIfNull(anomaly);

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _window.Invoke(() =>
        {
            try
            {
                if (!_gate.CanShow())
                {
                    completion.TrySetResult(false);
                    return;
                }

                var content = _composer.Compose(anomaly);
                completion.TrySetResult(_presenter.Show(content.Title, content.Body, content.Activation));
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        return completion.Task;
    }
}
