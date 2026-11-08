using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Yamori.Core.Anomalies;

namespace Yamori.ViewModels;

/// <summary>
/// 設定画面の「通知」に並べる、異常の種類ごとのオン・オフの 1 行。
/// </summary>
public sealed class AnomalyKindNotificationOption : ObservableObject
{
    private readonly Action<AnomalyKindNotificationOption, bool> _changed;
    private bool _isOn;
    private bool _isEditable;

    /// <summary>
    /// 種類と表示の文字列を指定して作る。
    /// </summary>
    /// <param name="kind">異常の種類。</param>
    /// <param name="title">見出し。</param>
    /// <param name="description">説明。</param>
    /// <param name="changed">利用者がオン・オフを変えたときに呼ぶ処理（新しい値を渡す）。</param>
    /// <exception cref="ArgumentNullException">いずれかの参照型の引数が null。</exception>
    public AnomalyKindNotificationOption(AnomalyKind kind, string title, string description, Action<AnomalyKindNotificationOption, bool> changed)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(changed);
        Kind = kind;
        Title = title;
        Description = description;
        _changed = changed;
    }

    /// <summary>異常の種類を取得する。</summary>
    public AnomalyKind Kind { get; }

    /// <summary>見出しを取得する。</summary>
    public string Title { get; }

    /// <summary>説明を取得する。</summary>
    public string Description { get; }

    /// <summary>
    /// この種類の通知を出すかどうかを取得または設定する。画面から変えると Helper に保存する。
    /// </summary>
    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (SetProperty(ref _isOn, value))
            {
                _changed(this, value);
            }
        }
    }

    /// <summary>変えられるかどうか（Helper につながっていて、通知の全体がオン）を取得する。</summary>
    public bool IsEditable
    {
        get => _isEditable;
        internal set => SetProperty(ref _isEditable, value);
    }

    /// <summary>
    /// Helper から読んだ値を、保存し直さずに反映する。
    /// </summary>
    /// <param name="isOn">通知を出すなら true。</param>
    internal void Load(bool isOn)
    {
        if (_isOn != isOn)
        {
            _isOn = isOn;
            OnPropertyChanged(nameof(IsOn));
        }
    }
}
