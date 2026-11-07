namespace Yamori.Core.Hardware;

/// <summary>
/// 冷却装置の種類。値は SMBIOS の Cooling Device（Type 27）の「Device Type」に合わせている。
/// </summary>
public enum CoolingDeviceType
{
    /// <summary>分からない。</summary>
    Unknown = 2,

    /// <summary>その他。</summary>
    Other = 1,

    /// <summary>ファン。</summary>
    Fan = 3,

    /// <summary>ブロワー（遠心ファン）。</summary>
    CentrifugalBlower = 4,

    /// <summary>チップ（CPU など）用のファン。</summary>
    ChipFan = 5,

    /// <summary>ケースファン。</summary>
    CabinetFan = 6,

    /// <summary>電源ユニットのファン。</summary>
    PowerSupplyFan = 7,

    /// <summary>ヒートパイプ。</summary>
    HeatPipe = 8,

    /// <summary>冷凍機（コンプレッサー式の冷却）。</summary>
    IntegratedRefrigeration = 9,

    /// <summary>能動的な冷却（種類は不明）。</summary>
    ActiveCooling = 16,

    /// <summary>受動的な冷却（ヒートシンクなど）。</summary>
    PassiveCooling = 17,
}
