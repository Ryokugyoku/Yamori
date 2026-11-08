namespace Yamori.Controls;

/// <summary>
/// グラフの系列の目盛りを、どちら側の軸に書くか。
/// </summary>
public enum ChartAxisSide
{
    /// <summary>左の軸（横の補助線もこの目盛りで引く）。</summary>
    Left,

    /// <summary>右の軸。</summary>
    Right,

    /// <summary>軸に書かない（凡例と、指したところの値だけで示す）。</summary>
    Hidden,
}
