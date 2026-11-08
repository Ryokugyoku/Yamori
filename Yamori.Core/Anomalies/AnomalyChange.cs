namespace Yamori.Core.Anomalies;

/// <summary>
/// 異常の知らせの種類（何が変わったか）。
/// </summary>
/// <remarks>
/// IPC で数値として送るため、既存の値の番号は変えないこと。追加は末尾に行う。知らない値は、受け取った側で無視する。
/// </remarks>
public enum AnomalyChange
{
    /// <summary>新しい異常が始まった。</summary>
    Started = 0,

    /// <summary>続いている異常が重くなった。</summary>
    Escalated = 1,

    /// <summary>異常が終わった。</summary>
    Ended = 2,

    /// <summary>「これは正常」の印が付いた、または外れた。</summary>
    DismissalChanged = 3,
}
