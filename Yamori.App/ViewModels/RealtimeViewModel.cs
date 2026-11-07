using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Yamori.Core.Realtime;
using Yamori.Localization;
using Yamori.Presentation.Realtime;

namespace Yamori.ViewModels;

/// <summary>
/// リアルタイム画面の状態。画面を開いている間だけ 1 秒ごとに測り、部品の種類ごとのカードを書き換える。
/// </summary>
public sealed class RealtimeViewModel : ObservableObject, IDisposable
{
    private readonly Func<IRealtimeSnapshotSource> _createSource;
    private readonly RealtimeSectionsPresenter _presenter;
    private readonly TimeProvider _timeProvider;
    private readonly SynchronizationContext _uiContext;
    private CancellationTokenSource? _cancellation;
    private string _statusText;

    /// <summary>
    /// 必要な処理を指定して作る。UI スレッドから呼ぶこと（測定結果を UI スレッドへ戻すため）。
    /// </summary>
    /// <param name="createSource">測定の処理を作る処理。測定を始めるたびに呼び、止めたら破棄する。</param>
    /// <param name="presenter">測定結果をカードの表示内容にする処理。</param>
    /// <param name="timeProvider">タイマーの提供元。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    /// <exception cref="InvalidOperationException">UI スレッド以外から呼ばれた。</exception>
    public RealtimeViewModel(Func<IRealtimeSnapshotSource> createSource, RealtimeSectionsPresenter presenter, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(createSource);
        ArgumentNullException.ThrowIfNull(presenter);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _createSource = createSource;
        _presenter = presenter;
        _timeProvider = timeProvider;
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException("RealtimeViewModel は UI スレッドで作成してください。");
        _statusText = UiText.Get("Realtime_Waiting");
    }

    /// <summary>部品の種類ごとのカードを取得する。</summary>
    public ObservableCollection<RealtimeSection> Sections { get; } = [];

    /// <summary>最後に測った時刻などの状態表示を取得する。</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// 測定を始める。すでに測定中なら何もしない。
    /// </summary>
    public void Start()
    {
        if (_cancellation is not null)
        {
            return;
        }

        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;

        // パフォーマンスカウンターの準備や WMI の読み取りは時間がかかるため、UI スレッドの外で行う。
        _ = Task.Run(() => RunAsync(token), token);
    }

    /// <summary>
    /// 測定を止める。止めた後に届いた測定結果は画面に反映しない。
    /// </summary>
    public void Stop()
    {
        if (_cancellation is null)
        {
            return;
        }

        _cancellation.Cancel();
        _cancellation.Dispose();
        _cancellation = null;
    }

    /// <summary>
    /// 測定を止める。
    /// </summary>
    public void Dispose() => Stop();

    private async Task RunAsync(CancellationToken token)
    {
        IRealtimeSnapshotSource source;
        try
        {
            source = _createSource();
        }
        catch (InvalidOperationException)
        {
            // パフォーマンスカウンターが壊れている PC など。画面には測れないことだけを示す。
            _uiContext.Post(_ => StatusText = UiText.Get("Realtime_Unavailable"), null);
            return;
        }

        using (source)
        {
            var monitor = new RealtimeMonitor(source, _timeProvider);
            monitor.SnapshotTaken += (_, snapshot) => _uiContext.Post(_ => Apply(snapshot, token), null);
            await monitor.RunAsync(token).ConfigureAwait(false);
        }
    }

    private void Apply(RealtimeSnapshot snapshot, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        KeyedCollectionSync.Sync(
            Sections,
            _presenter.Build(snapshot),
            s => s.Key,
            d => d.Key,
            d => new RealtimeSection(d),
            (s, d) => s.Update(d));
        StatusText = UiText.Format("Realtime_Updated", snapshot.Timestamp.ToLocalTime().ToString("T"));
    }
}
