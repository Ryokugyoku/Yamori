namespace Yamori.Core.Hardware;

/// <summary>
/// 今使っている電源の種類。
/// </summary>
public enum PowerSource
{
    /// <summary>分からない。</summary>
    Unknown,

    /// <summary>コンセント（AC 電源）。</summary>
    Ac,

    /// <summary>バッテリー。</summary>
    Battery,
}
