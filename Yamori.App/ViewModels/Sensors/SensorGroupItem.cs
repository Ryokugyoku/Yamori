using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// 詳細のツリーの 2 段目（グループ）の表示内容。リアルタイム画面では 1 枚のカードになる。
/// </summary>
public sealed class SensorGroupItem : ObservableObject
{
    private string _title = string.Empty;
    private string _note = string.Empty;
    private string _countText = string.Empty;
    private bool _isDense;

    /// <summary>
    /// グループのキーを指定して作る。
    /// </summary>
    /// <param name="key">グループを識別するキー。</param>
    public SensorGroupItem(string key) => Key = key;

    /// <summary>グループを識別するキーを取得する。</summary>
    public string Key { get; }

    /// <summary>グループの名前を取得する。</summary>
    public string Title { get => _title; set => SetProperty(ref _title, value); }

    /// <summary>
    /// 見出しの横に出す補足（センサーの名前から取り除いた共通の末尾など）を取得する。なければ空。
    /// </summary>
    public string Note
    {
        get => _note;
        set
        {
            if (SetProperty(ref _note, value))
            {
                OnPropertyChanged(nameof(HasNote));
            }
        }
    }

    /// <summary>補足があるかどうかを取得する。</summary>
    public bool HasNote => Note.Length > 0;

    /// <summary>センサーの数の表示を取得する。</summary>
    public string CountText { get => _countText; set => SetProperty(ref _countText, value); }

    /// <summary>
    /// センサーの数が多く、小さなマスで詰めて並べるかどうかを取得する。
    /// </summary>
    public bool IsDense
    {
        get => _isDense;
        set
        {
            if (SetProperty(ref _isDense, value))
            {
                OnPropertyChanged(nameof(IsList));
            }
        }
    }

    /// <summary>名前と値を 1 行ずつ並べるかどうか（<see cref="IsDense"/> の反対）を取得する。</summary>
    public bool IsList => !IsDense;

    /// <summary>グループに属するセンサーを取得する。</summary>
    public ObservableCollection<SensorRowItem> Rows { get; } = [];
}
