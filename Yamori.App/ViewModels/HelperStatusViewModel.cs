using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Ipc.Client;
using Yamori.Localization;
using Yamori.Presentation;
using Yamori.Presentation.Sensors;
using Yamori.Services;
using Yamori.ViewModels.Sensors;

namespace Yamori.ViewModels;

/// <summary>
/// タイトルバーに常に出す「見守りの状態」（Helper とつながって記録しているか）。どの画面からでも状態を確かめ、止まっていればサービスを起動できる。
/// </summary>
public sealed class HelperStatusViewModel : ObservableObject, IDisposable
{
    private readonly SensorBoard _board;
    private readonly IHelperServiceStarter _starter;
    private HelperConnectionState _state;
    private ExtendedSensorState _extended;
    private bool _isStarting;

    /// <summary>
    /// 共有の状態と、サービスを起動する処理を指定して作る。UI スレッドから呼ぶこと。
    /// </summary>
    /// <param name="board">センサーの今の状態。</param>
    /// <param name="starter">止まっている Helper のサービスを起動する処理。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public HelperStatusViewModel(SensorBoard board, IHelperServiceStarter starter)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(starter);
        _board = board;
        _starter = starter;
        _state = board.ConnectionState;
        _extended = board.ExtendedState;
        _board.Updated += OnUpdated;
    }

    /// <summary>札の色分けを取得する。</summary>
    public StatusLevel Level => _state switch
    {
        HelperConnectionState.Connected => StatusLevel.Good,
        HelperConnectionState.Connecting => StatusLevel.Attention,
        _ => StatusLevel.Caution,
    };

    /// <summary>札の短い文字（例：「見守り中」）を取得する。</summary>
    public string Label => UiText.Get("HelperStatus_" + _state);

    /// <summary>詳しい説明を取得する。</summary>
    public string Detail => _state == HelperConnectionState.Connected
        ? UiText.Get("HelperStatus_ConnectedDetail")
        : SensorTexts.HelperConnection(_state);

    /// <summary>拡張センサー（PawnIO）の状態の説明を取得する。Helper とつながっていないときは空。</summary>
    public string ExtendedText => _state == HelperConnectionState.Connected
        ? SensorTexts.ExtendedState(_extended)
        : string.Empty;

    /// <summary>拡張センサーの状態の説明を出すかどうかを取得する。</summary>
    public bool HasExtendedText => ExtendedText.Length > 0;

    /// <summary>拡張センサーの見出しを取得する。</summary>
    public string ExtendedTitle => SensorTexts.ExtendedTitle;

    /// <summary>サービスを起動するボタンを出すかどうか（Helper が動いていないとき）を取得する。</summary>
    public bool CanStart => _state == HelperConnectionState.NotRunning;

    /// <summary>サービスを起動するボタンを押せるかどうか（起動している最中は押せない）を取得する。</summary>
    public bool IsStartEnabled => !_isStarting;

    /// <summary>サービスを起動するボタンの文字列を取得する。</summary>
    public string StartText => SensorTexts.StartHelperAction;

    /// <summary>
    /// Helper のサービスを起動する。権限が足りなければ、Windows の管理者の承認（UAC）を得て起動だけを管理者の権限で行う。
    /// </summary>
    /// <returns>起動できなかった場合は、その理由の説明。起動できた場合と、承認の画面で取り消された場合は null。</returns>
    public async Task<string?> StartAsync()
    {
        SetStarting(true);
        try
        {
            var result = await _starter.StartAsync();
            return result is HelperStartResult.Started or HelperStartResult.Cancelled ? null : SensorTexts.StartHelperResult(result);
        }
        finally
        {
            SetStarting(false);
        }
    }

    /// <summary>
    /// 共有の状態の購読を解除する。
    /// </summary>
    public void Dispose() => _board.Updated -= OnUpdated;

    private void OnUpdated(object? sender, EventArgs e)
    {
        // 値を読むたびにも呼ばれるため、状態が変わったときだけ画面に知らせる。
        if (_state == _board.ConnectionState && _extended == _board.ExtendedState)
        {
            return;
        }

        _state = _board.ConnectionState;
        _extended = _board.ExtendedState;
        OnPropertyChanged(nameof(Level));
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Detail));
        OnPropertyChanged(nameof(ExtendedText));
        OnPropertyChanged(nameof(HasExtendedText));
        OnPropertyChanged(nameof(CanStart));
    }

    private void SetStarting(bool value)
    {
        if (_isStarting != value)
        {
            _isStarting = value;
            OnPropertyChanged(nameof(IsStartEnabled));
        }
    }
}
