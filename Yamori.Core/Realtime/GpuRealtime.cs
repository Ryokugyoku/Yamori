using Yamori.Core.Realtime.Gpu;

namespace Yamori.Core.Realtime;

/// <summary>
/// GPU 1 つの今の状態。測れなかった値は null。
/// </summary>
/// <param name="Luid">アダプターの LUID。</param>
/// <param name="Name">アダプターの名前。</param>
public sealed record GpuRealtime(ulong Luid, string Name)
{
    /// <summary>GPU 全体の使用率（%）を取得する。最も使われているエンジンの使用率で、タスク マネージャーと同じ考え方。</summary>
    public double? UsagePercent { get; init; }

    /// <summary>エンジンの種類ごとの使用率を、使用率の高い順に取得する。</summary>
    public IReadOnlyList<GpuEngineUsage> Engines { get; init; } = [];

    /// <summary>専用ビデオメモリの使用量（バイト）を取得する。</summary>
    public ulong? DedicatedUsedBytes { get; init; }

    /// <summary>共有メモリ（システムメモリを借りている分）の使用量（バイト）を取得する。</summary>
    public ulong? SharedUsedBytes { get; init; }

    /// <summary>コミット済みの GPU メモリの合計（バイト）を取得する。</summary>
    public ulong? TotalCommittedBytes { get; init; }

    /// <summary>ドライバーが報告しているセンサーの値（温度、ファン、電力、クロック）を取得する。</summary>
    public GpuTelemetry? Telemetry { get; init; }
}
