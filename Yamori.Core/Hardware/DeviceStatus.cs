namespace Yamori.Core.Hardware;

/// <summary>
/// ファームウェア（SMBIOS）が報告する装置の状態。値は SMBIOS の状態の値に合わせている。
/// </summary>
public enum DeviceStatus
{
    /// <summary>その他。</summary>
    Other = 1,

    /// <summary>分からない。</summary>
    Unknown = 2,

    /// <summary>正常。</summary>
    Ok = 3,

    /// <summary>軽い異常。</summary>
    NonCritical = 4,

    /// <summary>重大な異常。</summary>
    Critical = 5,

    /// <summary>回復できない異常。</summary>
    NonRecoverable = 6,
}
