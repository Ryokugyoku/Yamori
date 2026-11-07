namespace Yamori.Core.Sensors.Extended;

/// <summary>
/// PawnIO（ユーザーが入れるドライバー）がこの PC に入っているかどうかを調べる。調べるだけで、ドライバーには触れない。
/// </summary>
public interface IPawnIoInstallationProbe
{
    /// <summary>
    /// PawnIO が入っているかどうかを調べる。
    /// </summary>
    /// <returns>入っていれば true。</returns>
    bool IsInstalled();
}
