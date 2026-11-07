namespace Yamori.Core.Sensors.Extended;

/// <summary>
/// 入っている PawnIO のドライバーが公式版（Official Build）かどうかを確かめる。確かめるだけで、ドライバーには触れない。
/// </summary>
public interface IPawnIoEditionVerifier
{
    /// <summary>
    /// 公式版かどうかを確かめる。
    /// </summary>
    /// <returns>公式版だと確かめられたら true。確かめられない場合（署名がない、Unrestricted 版など）は false。</returns>
    bool IsOfficial();
}
