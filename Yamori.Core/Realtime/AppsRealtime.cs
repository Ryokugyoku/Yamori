using Yamori.Core.Processes;

namespace Yamori.Core.Realtime;

/// <summary>
/// アプリごとの今の使用量。CPU・GPU・入出力のそれぞれで、多く使っているアプリを上位から並べる。
/// </summary>
/// <param name="TopByCpu">CPU を多く使っているアプリ。</param>
/// <param name="TopByGpu">GPU を多く使っているアプリ。</param>
/// <param name="TopByIo">読み書き（入出力）の多いアプリ。</param>
public sealed record AppsRealtime(
    IReadOnlyList<AppActivity> TopByCpu,
    IReadOnlyList<AppActivity> TopByGpu,
    IReadOnlyList<AppActivity> TopByIo)
{
    /// <summary>
    /// 何も測れなかったことを表す値。
    /// </summary>
    public static AppsRealtime Empty { get; } = new([], [], []);
}
