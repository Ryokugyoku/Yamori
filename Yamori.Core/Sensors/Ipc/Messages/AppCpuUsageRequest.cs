namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// アプリごとの CPU 使用率の記録のまとめの取得の依頼。
/// </summary>
/// <param name="FromUtc">期間の始まり（含む）。</param>
/// <param name="ToUtc">期間の終わり（含まない）。</param>
/// <param name="HighLoadThresholdPercent">高負荷とみなす PC 全体の CPU 使用率（%）。</param>
/// <param name="Top">それぞれの順位で返す上位の数。</param>
public sealed record AppCpuUsageRequest(DateTimeOffset FromUtc, DateTimeOffset ToUtc, double HighLoadThresholdPercent, int Top);
