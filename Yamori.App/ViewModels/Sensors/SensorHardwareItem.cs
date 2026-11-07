using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// 詳細のツリーの 1 段目（ハードウェア）の表示内容。
/// </summary>
public sealed class SensorHardwareItem : ObservableObject
{
    private string _glyph = string.Empty;
    private string _title = string.Empty;
    private string _kindName = string.Empty;

    /// <summary>
    /// ハードウェアのキーを指定して作る。
    /// </summary>
    /// <param name="key">ハードウェアを識別するキー。</param>
    public SensorHardwareItem(string key) => Key = key;

    /// <summary>ハードウェアを識別するキーを取得する。</summary>
    public string Key { get; }

    /// <summary>アイコンを取得する。</summary>
    public string Glyph { get => _glyph; set => SetProperty(ref _glyph, value); }

    /// <summary>ハードウェアの名前を取得する。</summary>
    public string Title { get => _title; set => SetProperty(ref _title, value); }

    /// <summary>ハードウェアの種類の名前を取得する。</summary>
    public string KindName { get => _kindName; set => SetProperty(ref _kindName, value); }

    /// <summary>グループを取得する。</summary>
    public ObservableCollection<SensorGroupItem> Groups { get; } = [];
}
