namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// 今の電源の状態（AC 電源かバッテリーか、残量など）を読み取る。
/// </summary>
public interface IPowerStatusSource
{
    /// <summary>
    /// 今の状態を読み取る。
    /// </summary>
    /// <returns>今の状態。読めない場合は null。</returns>
    PowerStatus? Read();
}
