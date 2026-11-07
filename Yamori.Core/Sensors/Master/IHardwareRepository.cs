namespace Yamori.Core.Sensors.Master;

/// <summary>
/// マスタの <c>hardware</c> を読み書きする。
/// </summary>
public interface IHardwareRepository
{
    /// <summary>
    /// ハードウェアを追加するか、名前と最後に見つけた時刻を更新する。
    /// </summary>
    /// <param name="stableKey">ハードウェアを識別するキー。</param>
    /// <param name="kind">ハードウェアの種類。</param>
    /// <param name="name">表示名。</param>
    /// <param name="seenAtUtc">見つけた時刻。</param>
    /// <returns>追加・更新した行。</returns>
    HardwareRecord Upsert(string stableKey, HardwareKind kind, string name, DateTimeOffset seenAtUtc);

    /// <summary>
    /// すべてのハードウェアを、行の番号の順に取得する。
    /// </summary>
    /// <returns>すべての行。</returns>
    IReadOnlyList<HardwareRecord> GetAll();
}
