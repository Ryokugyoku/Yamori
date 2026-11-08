using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Yamori.Presentation.Realtime;

/// <summary>
/// 画面に表示している項目 1 つ。1 秒ごとの更新では作り直さず、値だけを書き換える（ちらつきを防ぐため）。
/// </summary>
public sealed class RealtimeFact : ObservableObject
{
    private string _label;
    private string _value;
    private double _percent;
    private bool _hasPercent;
    private string _detail;

    /// <summary>
    /// 最初の表示内容を指定して作る。
    /// </summary>
    /// <param name="data">表示内容。</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> が null。</exception>
    public RealtimeFact(RealtimeFactData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Key = data.Key;
        _label = data.Label;
        _value = data.Value;
        _percent = data.Percent ?? 0;
        _hasPercent = data.Percent is not null;
        _detail = data.Detail ?? string.Empty;
    }

    /// <summary>同じ項目を見分けるためのキーを取得する。</summary>
    public string Key { get; }

    /// <summary>項目名を取得する。</summary>
    public string Label
    {
        get => _label;
        private set => SetProperty(ref _label, value);
    }

    /// <summary>値の表示を取得する。</summary>
    public string Value
    {
        get => _value;
        private set => SetProperty(ref _value, value);
    }

    /// <summary>棒グラフで示す割合（0〜100）を取得する。</summary>
    public double Percent
    {
        get => _percent;
        private set => SetProperty(ref _percent, value);
    }

    /// <summary>棒グラフを出すかどうかを取得する。</summary>
    public bool HasPercent
    {
        get => _hasPercent;
        private set => SetProperty(ref _hasPercent, value);
    }

    /// <summary>項目名の下に小さく出す補足を取得する。なければ空。</summary>
    public string Detail
    {
        get => _detail;
        private set
        {
            if (SetProperty(ref _detail, value))
            {
                OnPropertyChanged(nameof(HasDetail));
            }
        }
    }

    /// <summary>補足があるかどうかを取得する。</summary>
    public bool HasDetail => Detail.Length > 0;

    /// <summary>
    /// 新しい表示内容で書き換える。
    /// </summary>
    /// <param name="data">新しい表示内容。キーは同じであること。</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> が null。</exception>
    public void Update(RealtimeFactData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Label = data.Label;
        Value = data.Value;
        Percent = data.Percent ?? 0;
        HasPercent = data.Percent is not null;
        Detail = data.Detail ?? string.Empty;
    }
}
