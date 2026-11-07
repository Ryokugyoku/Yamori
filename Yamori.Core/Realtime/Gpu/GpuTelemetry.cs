namespace Yamori.Core.Realtime.Gpu;

/// <summary>
/// GPU のドライバーが Windows に報告している、今のセンサーの値。ドライバーが公開していない値は null。
/// </summary>
/// <param name="Luid">アダプターの LUID。パフォーマンスカウンターのインスタンス名と結びつけるのに使う。</param>
/// <param name="Name">アダプターの名前。</param>
public sealed record GpuTelemetry(ulong Luid, string Name)
{
    /// <summary>
    /// PCI バス上の位置（<c>bus:device.function</c> の 16 進数。例：<c>01:00.0</c>）を取得する。LUID と違い再起動しても変わらない。
    /// 読めない場合（仮想のアダプターなど）は null。
    /// </summary>
    public string? PciLocation { get; init; }

    /// <summary>専用ビデオメモリの大きさ（バイト）を取得する。</summary>
    public ulong? DedicatedMemoryBytes { get; init; }

    /// <summary>共有できるシステムメモリの大きさ（バイト）を取得する。</summary>
    public ulong? SharedMemoryBytes { get; init; }

    /// <summary>温度（℃）を取得する。</summary>
    public double? TemperatureCelsius { get; init; }

    /// <summary>温度の上限（℃）を取得する。</summary>
    public double? MaxTemperatureCelsius { get; init; }

    /// <summary>ファンの回転数（RPM）を取得する。</summary>
    public int? FanRpm { get; init; }

    /// <summary>ファンの最大回転数（RPM）を取得する。</summary>
    public int? MaxFanRpm { get; init; }

    /// <summary>消費電力の、定格（TDP）に対する割合（%）を取得する。</summary>
    public double? PowerPercent { get; init; }

    /// <summary>GPU コアの今の動作クロック（MHz）を取得する。</summary>
    public double? CoreClockMhz { get; init; }

    /// <summary>GPU コアの最大動作クロック（MHz）を取得する。</summary>
    public double? MaxCoreClockMhz { get; init; }

    /// <summary>GPU コアの今の電圧（mV）を取得する。</summary>
    public int? CoreVoltageMillivolts { get; init; }

    /// <summary>ビデオメモリの今の動作クロック（MHz）を取得する。</summary>
    public double? MemoryClockMhz { get; init; }

    /// <summary>ビデオメモリの最大動作クロック（MHz）を取得する。</summary>
    public double? MaxMemoryClockMhz { get; init; }

    /// <summary>ビデオメモリの帯域の使用量（バイト／秒）を取得する。</summary>
    public ulong? MemoryBandwidthBytesPerSecond { get; init; }

    /// <summary>ビデオメモリの最大帯域（バイト／秒）を取得する。</summary>
    public ulong? MaxMemoryBandwidthBytesPerSecond { get; init; }

    /// <summary>PCI Express の帯域の使用量（バイト／秒）を取得する。</summary>
    public ulong? PcieBandwidthBytesPerSecond { get; init; }

    /// <summary>PCI Express の最大帯域（バイト／秒）を取得する。</summary>
    public ulong? MaxPcieBandwidthBytesPerSecond { get; init; }
}
