using Yamori.Core.Realtime;

namespace Yamori.Presentation.Realtime;

/// <summary>
/// 測定結果から、部品の種類 1 つ分のカードの表示内容を作る。
/// </summary>
public interface IRealtimeSectionBuilder
{
    /// <summary>
    /// 表示内容を作る。
    /// </summary>
    /// <param name="snapshot">測定結果。</param>
    /// <returns>カードの表示内容。</returns>
    RealtimeSectionData Build(RealtimeSnapshot snapshot);
}
