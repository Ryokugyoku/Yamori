using CommunityToolkit.Mvvm.ComponentModel;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// 詳細のツリーの 3 段目（センサー 1 つ）の表示内容。更新では作り直さず、中身を書き換える。
/// </summary>
public sealed class SensorRowItem : ObservableObject
{
    private string _label = string.Empty;
    private string _valueText = string.Empty;
    private double _percent;
    private bool _hasPercent;
    private double _opacity = 1;

    /// <summary>
    /// センサーのキーを指定して作る。
    /// </summary>
    /// <param name="key">センサーのキーの文字列表記。</param>
    public SensorRowItem(string key) => Key = key;

    /// <summary>センサーのキーの文字列表記を取得する。</summary>
    public string Key { get; }

    /// <summary>センサーの名前を取得する。</summary>
    public string Label { get => _label; set => SetProperty(ref _label, value); }

    /// <summary>値の表示を取得する。読めないときは「—」。</summary>
    public string ValueText { get => _valueText; set => SetProperty(ref _valueText, value); }

    /// <summary>棒グラフで示す割合（0〜100）を取得する。</summary>
    public double Percent { get => _percent; set => SetProperty(ref _percent, value); }

    /// <summary>棒グラフを出すかどうかを取得する。</summary>
    public bool HasPercent { get => _hasPercent; set => SetProperty(ref _hasPercent, value); }

    /// <summary>不透明度を取得する。一時的に読めないときは薄くする。</summary>
    public double Opacity { get => _opacity; set => SetProperty(ref _opacity, value); }
}
