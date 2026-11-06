namespace Yamori.Core.Hardware;

/// <summary>
/// Windows が報告するストレージの健康状態。値は MSFT_PhysicalDisk.HealthStatus に合わせている。
/// </summary>
public enum StorageHealth
{
    /// <summary>正常。</summary>
    Healthy = 0,

    /// <summary>注意が必要。</summary>
    Warning = 1,

    /// <summary>異常。</summary>
    Unhealthy = 2,

    /// <summary>状態が分からない。</summary>
    Unknown = 5,
}
