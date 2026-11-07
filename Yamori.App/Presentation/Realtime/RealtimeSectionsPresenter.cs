using System;
using System.Collections.Generic;
using System.Linq;
using Yamori.Core.Realtime;

namespace Yamori.Presentation.Realtime;

/// <summary>
/// 測定結果を、部品の種類ごとのカード（CPU、RAM、GPU、HDD、電源ユニット、ファン）の表示内容にする。
/// </summary>
public sealed class RealtimeSectionsPresenter
{
    private readonly IReadOnlyList<IRealtimeSectionBuilder> _builders;

    /// <summary>
    /// カードを作る処理を、表示する順に指定して作る。
    /// </summary>
    /// <param name="builders">カードを作る処理。</param>
    /// <exception cref="ArgumentNullException"><paramref name="builders"/> が null。</exception>
    public RealtimeSectionsPresenter(IEnumerable<IRealtimeSectionBuilder> builders)
    {
        ArgumentNullException.ThrowIfNull(builders);
        _builders = builders.ToArray();
    }

    /// <summary>
    /// アプリごとの使用量のカードだけを作る構成で作る。部品ごとのセンサーは、カタログから組み立てるツリーで表示する。
    /// </summary>
    /// <returns>新しいインスタンス。</returns>
    public static RealtimeSectionsPresenter CreateDefault() => new([new AppsRealtimeSectionBuilder()]);

    /// <summary>
    /// カードの表示内容を作る。
    /// </summary>
    /// <param name="snapshot">測定結果。</param>
    /// <returns>表示する順のカードの表示内容。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> が null。</exception>
    public IReadOnlyList<RealtimeSectionData> Build(RealtimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return _builders.Select(b => b.Build(snapshot)).ToArray();
    }
}
