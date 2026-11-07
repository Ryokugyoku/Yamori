using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Yamori.Presentation.Realtime;

/// <summary>
/// 画面に表示している部品の種類 1 つ分（例：CPU）のカード。更新では作り直さず、中身を書き換える。
/// </summary>
public sealed class RealtimeSection : ObservableObject
{
    private string _glyph = string.Empty;
    private string _category = string.Empty;
    private string _headline = string.Empty;
    private double _headlinePercent;
    private bool _hasHeadlinePercent;
    private string _note = string.Empty;

    /// <summary>
    /// 最初の表示内容を指定して作る。
    /// </summary>
    /// <param name="data">表示内容。</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> が null。</exception>
    public RealtimeSection(RealtimeSectionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Key = data.Key;
        Update(data);
    }

    /// <summary>同じ種類を見分けるためのキーを取得する。</summary>
    public string Key { get; }

    /// <summary>部品の種類を表すアイコンを取得する。</summary>
    public string Glyph
    {
        get => _glyph;
        private set => SetProperty(ref _glyph, value);
    }

    /// <summary>部品の種類の名前を取得する。</summary>
    public string Category
    {
        get => _category;
        private set => SetProperty(ref _category, value);
    }

    /// <summary>見出しに大きく出す値を取得する。</summary>
    public string Headline
    {
        get => _headline;
        private set => SetProperty(ref _headline, value);
    }

    /// <summary>見出しの下の棒グラフで示す割合（0〜100）を取得する。</summary>
    public double HeadlinePercent
    {
        get => _headlinePercent;
        private set => SetProperty(ref _headlinePercent, value);
    }

    /// <summary>見出しの下に棒グラフを出すかどうかを取得する。</summary>
    public bool HasHeadlinePercent
    {
        get => _hasHeadlinePercent;
        private set => SetProperty(ref _hasHeadlinePercent, value);
    }

    /// <summary>数値の意味や、測れない値についての説明を取得する。</summary>
    public string Note
    {
        get => _note;
        private set
        {
            if (SetProperty(ref _note, value))
            {
                OnPropertyChanged(nameof(HasNote));
            }
        }
    }

    /// <summary>説明があるかどうかを取得する。</summary>
    public bool HasNote => Note.Length > 0;

    /// <summary>この種類に属する部品を取得する。</summary>
    public ObservableCollection<RealtimeEntry> Entries { get; } = [];

    /// <summary>
    /// 新しい表示内容で書き換える。
    /// </summary>
    /// <param name="data">新しい表示内容。キーは同じであること。</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> が null。</exception>
    public void Update(RealtimeSectionData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Glyph = data.Glyph;
        Category = data.Category;
        Headline = data.Headline;
        HeadlinePercent = data.HeadlinePercent ?? 0;
        HasHeadlinePercent = data.HeadlinePercent is not null;
        Note = data.Note;
        KeyedCollectionSync.Sync(Entries, data.Entries, e => e.Key, d => d.Key, d => new RealtimeEntry(d), (e, d) => e.Update(d));
    }
}
