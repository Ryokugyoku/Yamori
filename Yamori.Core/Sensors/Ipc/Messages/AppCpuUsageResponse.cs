namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// パイプで送る <see cref="AppUsage.Analysis.AppCpuUsageSummary"/>。時間はミリ秒で送る。
/// </summary>
/// <param name="ObservedMilliseconds">記録していた時間の合計（ミリ秒）。</param>
/// <param name="HighLoadMilliseconds">そのうち高負荷だった時間（ミリ秒）。</param>
/// <param name="HighLoadThresholdPercent">高負荷とみなしたしきい値（%）。</param>
/// <param name="Apps">CPU を多く使っていたアプリ。</param>
public sealed record AppCpuUsageResponse(
    long ObservedMilliseconds,
    long HighLoadMilliseconds,
    double HighLoadThresholdPercent,
    AppCpuUsageDto[]? Apps);
