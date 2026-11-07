using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// 詳細のツリーの 2 段目（グループ）の表示内容。
/// </summary>
public sealed class SensorGroupItem : ObservableObject
{
    private string _title = string.Empty;

    /// <summary>
    /// グループのキーを指定して作る。
    /// </summary>
    /// <param name="key">グループを識別するキー。</param>
    public SensorGroupItem(string key) => Key = key;

    /// <summary>グループを識別するキーを取得する。</summary>
    public string Key { get; }

    /// <summary>グループの名前を取得する。</summary>
    public string Title { get => _title; set => SetProperty(ref _title, value); }

    /// <summary>グループに属するセンサーを取得する。</summary>
    public ObservableCollection<SensorRowItem> Rows { get; } = [];
}
