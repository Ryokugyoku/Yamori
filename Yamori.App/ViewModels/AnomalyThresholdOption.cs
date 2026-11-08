using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Yamori.Core.Anomalies;

namespace Yamori.ViewModels;

/// <summary>
/// 設定画面の「しきい値」に並べる、異常の種類ごとのしきい値の 1 行。「自動（おすすめ）」か「手動で指定」を選び、手動なら値を入れる。
/// </summary>
public sealed class AnomalyThresholdOption : ObservableObject
{
    /// <summary>「自動（おすすめ）」の選択肢の番号。</summary>
    public const int AutomaticIndex = 0;

    /// <summary>「手動で指定」の選択肢の番号。</summary>
    public const int ManualIndex = 1;

    private readonly Action<AnomalyThresholdOption> _changed;
    private bool _isManual;
    private double _value;
    private bool _isEditable;

    /// <summary>
    /// 種類と表示の文字列を指定して作る。
    /// </summary>
    /// <param name="kind">異常の種類。</param>
    /// <param name="title">見出し。</param>
    /// <param name="description">自動のときに使う値の説明と、手動の値の意味。</param>
    /// <param name="unit">値の単位（例：「℃」）。</param>
    /// <param name="changed">利用者が自動・手動か値を変えたときに呼ぶ処理。</param>
    /// <exception cref="ArgumentNullException">いずれかの参照型の引数が null。</exception>
    public AnomalyThresholdOption(AnomalyKind kind, string title, string description, string unit, Action<AnomalyThresholdOption> changed)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(changed);
        Kind = kind;
        Title = title;
        Description = description;
        Unit = unit;
        _changed = changed;
        (Minimum, Maximum) = AnomalySettingRules.ThresholdRange(kind);
        _value = AnomalySettingRules.InitialManualThreshold(kind);
    }

    /// <summary>異常の種類を取得する。</summary>
    public AnomalyKind Kind { get; }

    /// <summary>見出しを取得する。</summary>
    public string Title { get; }

    /// <summary>説明を取得する。</summary>
    public string Description { get; }

    /// <summary>値の単位を取得する。</summary>
    public string Unit { get; }

    /// <summary>手動で入れられる最小の値を取得する。</summary>
    public double Minimum { get; }

    /// <summary>手動で入れられる最大の値を取得する。</summary>
    public double Maximum { get; }

    /// <summary>
    /// 選んでいる選択肢の番号（<see cref="AutomaticIndex"/> か <see cref="ManualIndex"/>）を取得または設定する。画面から変えると Helper に保存する。
    /// </summary>
    public int ModeIndex
    {
        get => _isManual ? ManualIndex : AutomaticIndex;
        set
        {
            var manual = value == ManualIndex;
            if (value < 0 || manual == _isManual)
            {
                return;
            }

            _isManual = manual;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsManual));
            _changed(this);
        }
    }

    /// <summary>手動で指定しているかどうかを取得する。</summary>
    public bool IsManual => _isManual;

    /// <summary>
    /// 手動のしきい値を取得または設定する。画面から変えると Helper に保存する。空欄（NaN）にしたときは前の値に戻す。
    /// </summary>
    public double Value
    {
        get => _value;
        set
        {
            if (!double.IsFinite(value))
            {
                // 空欄にされたら、前の値を出し直す。
                OnPropertyChanged();
                return;
            }

            var normalized = Normalize(value);
            if (normalized == _value)
            {
                if (normalized != value)
                {
                    OnPropertyChanged();
                }

                return;
            }

            _value = normalized;
            OnPropertyChanged();
            if (_isManual)
            {
                _changed(this);
            }
        }
    }

    /// <summary>保存するしきい値（自動なら null）を取得する。</summary>
    public double? ManualThreshold => _isManual ? _value : null;

    /// <summary>変えられるかどうか（Helper につながっている）を取得する。</summary>
    public bool IsEditable
    {
        get => _isEditable;
        internal set => SetProperty(ref _isEditable, value);
    }

    /// <summary>
    /// Helper から読んだ設定を、保存し直さずに反映する。
    /// </summary>
    /// <param name="manualThreshold">手動のしきい値。自動なら null。</param>
    internal void Load(double? manualThreshold)
    {
        var manual = manualThreshold is not null;
        var value = manualThreshold is { } threshold ? Normalize(threshold) : _value;
        if (manual != _isManual)
        {
            _isManual = manual;
            OnPropertyChanged(nameof(ModeIndex));
            OnPropertyChanged(nameof(IsManual));
        }

        if (value != _value)
        {
            _value = value;
            OnPropertyChanged(nameof(Value));
        }
    }

    private double Normalize(double value) =>
        AnomalySettingRules.WithManualThreshold(new AnomalySetting(Kind, true, null), value).ManualThreshold ?? _value;
}
