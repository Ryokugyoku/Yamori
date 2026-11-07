using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Yamori.Presentation.Realtime;

/// <summary>
/// 表示中の一覧を、新しい表示内容に合わせてキーで突き合わせて書き換える。
/// 同じキーの項目は作り直さずに更新するため、画面がちらつかず、スクロール位置も保たれる。
/// </summary>
public static class KeyedCollectionSync
{
    /// <summary>
    /// <paramref name="target"/> を <paramref name="source"/> と同じ並び・同じ中身にする。
    /// </summary>
    /// <typeparam name="TItem">表示中の項目の型。</typeparam>
    /// <typeparam name="TData">新しい表示内容の型。</typeparam>
    /// <param name="target">書き換える一覧。</param>
    /// <param name="source">新しい表示内容。キーは重複しないこと。</param>
    /// <param name="itemKey">表示中の項目のキーを返す処理。</param>
    /// <param name="dataKey">新しい表示内容のキーを返す処理。</param>
    /// <param name="create">新しい項目を作る処理。</param>
    /// <param name="update">既存の項目を書き換える処理。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static void Sync<TItem, TData>(
        ObservableCollection<TItem> target,
        IReadOnlyList<TData> source,
        Func<TItem, string> itemKey,
        Func<TData, string> dataKey,
        Func<TData, TItem> create,
        Action<TItem, TData> update)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(itemKey);
        ArgumentNullException.ThrowIfNull(dataKey);
        ArgumentNullException.ThrowIfNull(create);
        ArgumentNullException.ThrowIfNull(update);

        for (var i = 0; i < source.Count; i++)
        {
            var data = source[i];
            var key = dataKey(data);
            var existing = IndexOf(target, itemKey, key, i);
            if (existing < 0)
            {
                target.Insert(i, create(data));
                continue;
            }

            if (existing != i)
            {
                target.Move(existing, i);
            }

            update(target[i], data);
        }

        while (target.Count > source.Count)
        {
            target.RemoveAt(target.Count - 1);
        }
    }

    private static int IndexOf<TItem>(ObservableCollection<TItem> target, Func<TItem, string> itemKey, string key, int start)
    {
        for (var j = start; j < target.Count; j++)
        {
            if (string.Equals(itemKey(target[j]), key, StringComparison.Ordinal))
            {
                return j;
            }
        }

        return -1;
    }
}
