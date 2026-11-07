using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Yamori.Presentation.Realtime;

/// <summary>
/// 画面に表示している部品 1 つ（GPU 1 枚、ディスク 1 台など）。更新では作り直さず、中身を書き換える。
/// </summary>
public sealed class RealtimeEntry : ObservableObject
{
    private string _title = string.Empty;
    private string _subtitle = string.Empty;

    /// <summary>
    /// 最初の表示内容を指定して作る。
    /// </summary>
    /// <param name="data">表示内容。</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> が null。</exception>
    public RealtimeEntry(RealtimeEntryData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Key = data.Key;
        Update(data);
    }

    /// <summary>同じ部品を見分けるためのキーを取得する。</summary>
    public string Key { get; }

    /// <summary>部品の名前を取得する。</summary>
    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    /// <summary>名前の下に添える説明を取得する。</summary>
    public string Subtitle
    {
        get => _subtitle;
        private set
        {
            if (SetProperty(ref _subtitle, value))
            {
                OnPropertyChanged(nameof(HasSubtitle));
            }
        }
    }

    /// <summary>説明があるかどうかを取得する。</summary>
    public bool HasSubtitle => Subtitle.Length > 0;

    /// <summary>項目の一覧を取得する。</summary>
    public ObservableCollection<RealtimeFact> Facts { get; } = [];

    /// <summary>
    /// 新しい表示内容で書き換える。
    /// </summary>
    /// <param name="data">新しい表示内容。キーは同じであること。</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> が null。</exception>
    public void Update(RealtimeEntryData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Title = data.Title;
        Subtitle = data.Subtitle;
        KeyedCollectionSync.Sync(Facts, data.Facts, f => f.Key, d => d.Key, d => new RealtimeFact(d), (f, d) => f.Update(d));
    }
}
