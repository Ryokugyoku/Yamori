namespace Yamori.Core.Realtime;

/// <summary>
/// CPU の今の状態。測れなかった値は null。
/// </summary>
public sealed record CpuRealtime
{
    /// <summary>
    /// CPU 全体の使用率（%）を取得する。タスク マネージャーと同じ <c>% Processor Utility</c>（定格クロックでの処理能力に対する処理量）を
    /// 100 を上限にした値。読めない古い Windows では <see cref="ProcessorTimePercent"/> と同じ値。
    /// </summary>
    public double? UsagePercent { get; init; }

    /// <summary>
    /// CPU が動いていた時間の割合（%。<c>% Processor Time</c>）を取得する。クロックの高さを考えない値で、
    /// ブースト中は <see cref="UsagePercent"/> より低く、省電力でクロックが下がっているときは高く出る。
    /// </summary>
    public double? ProcessorTimePercent { get; init; }

    /// <summary>ユーザー モード（アプリ）で使った時間の割合（%）を取得する。</summary>
    public double? UserPercent { get; init; }

    /// <summary>カーネル モード（Windows・ドライバー）で使った時間の割合（%）を取得する。</summary>
    public double? PrivilegedPercent { get; init; }

    /// <summary>ハードウェア割り込みの処理に使った時間の割合（%）を取得する。</summary>
    public double? InterruptPercent { get; init; }

    /// <summary>遅延プロシージャ呼び出し（DPC）の処理に使った時間の割合（%）を取得する。</summary>
    public double? DpcPercent { get; init; }

    /// <summary>今の平均の動作クロック（MHz）を取得する。</summary>
    public double? ClockMhz { get; init; }

    /// <summary>定格クロック（MHz）を取得する。</summary>
    public double? BaseClockMhz { get; init; }

    /// <summary>定格クロックに対する今の動作クロックの割合（%）を取得する。100 を超えるとブースト中。</summary>
    public double? PerformancePercent { get; init; }

    /// <summary>電力や温度による制限の割合（%）を取得する。100 なら制限なし。</summary>
    public double? PerformanceLimitPercent { get; init; }

    /// <summary>実行を待っているスレッドの数を取得する。</summary>
    public double? QueueLength { get; init; }

    /// <summary>1 秒あたりのスレッドの切り替え回数を取得する。</summary>
    public double? ContextSwitchesPerSecond { get; init; }

    /// <summary>1 秒あたりのシステム コールの回数を取得する。</summary>
    public double? SystemCallsPerSecond { get; init; }

    /// <summary>1 秒あたりのハードウェア割り込みの回数を取得する。</summary>
    public double? InterruptsPerSecond { get; init; }

    /// <summary>プロセスの数を取得する。</summary>
    public int? ProcessCount { get; init; }

    /// <summary>スレッドの数を取得する。</summary>
    public int? ThreadCount { get; init; }

    /// <summary>開かれているハンドルの数を取得する。</summary>
    public int? HandleCount { get; init; }

    /// <summary>Windows を起動してからの時間を取得する。</summary>
    public TimeSpan? Uptime { get; init; }

    /// <summary>CPU パッケージ全体の消費電力（W）を取得する。</summary>
    public double? PackagePowerWatts { get; init; }

    /// <summary>CPU コアの消費電力（W）を取得する。</summary>
    public double? CoresPowerWatts { get; init; }

    /// <summary>論理プロセッサごとの状態を、番号の順に取得する。</summary>
    public IReadOnlyList<LogicalProcessorUsage> LogicalProcessors { get; init; } = [];
}
