namespace Yamori.Core.Sensors.Master;

/// <summary>
/// マスタの <c>source</c>（取得元）を読み書きする。時系列には Provider の ID の代わりに、ここで採番した番号を保存する。
/// </summary>
public interface ISourceRepository
{
    /// <summary>
    /// Provider の ID の番号を取得する。なければ採番する。
    /// </summary>
    /// <param name="providerId">Provider の ID。</param>
    /// <returns>取得元の番号。</returns>
    ushort GetOrCreate(string providerId);

    /// <summary>
    /// すべての取得元を取得する。
    /// </summary>
    /// <returns>Provider の ID から番号への対応。</returns>
    IReadOnlyDictionary<string, ushort> GetAll();
}
