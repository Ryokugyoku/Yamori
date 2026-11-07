namespace Yamori.Core.Hardware.Topology;

/// <summary>
/// CPU のコアの内訳（P コア・E コア）を提供する。テストで差し替えられるように抽象化している。
/// </summary>
public interface ICoreTopologySource
{
    /// <summary>
    /// コアの内訳を取得する。
    /// </summary>
    /// <returns>ハイブリッド構成の場合は内訳。そうでない場合や取得できない場合は null。</returns>
    CoreTopology? Read();
}
