namespace Yamori.Core.Sensors.Master;

/// <summary>
/// イベントの重さ。
/// </summary>
/// <remarks>マスタに数値で保存するため、既存の値の番号は変えないこと。</remarks>
public enum EventLevel
{
    /// <summary>お知らせ。</summary>
    Information = 0,

    /// <summary>注意（動作は続けている）。</summary>
    Warning = 1,

    /// <summary>失敗。</summary>
    Error = 2,
}
