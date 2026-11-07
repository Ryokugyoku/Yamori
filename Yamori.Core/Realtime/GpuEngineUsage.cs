namespace Yamori.Core.Realtime;

/// <summary>
/// GPU のエンジンの種類ごとの今の使用率。
/// </summary>
/// <param name="EngineType">エンジンの種類（<c>3D</c>、<c>Copy</c>、<c>VideoDecode</c>、<c>VideoEncode</c>、<c>Compute_0</c> など、ドライバーが付けた名前）。</param>
/// <param name="UsagePercent">その種類のエンジンのうち、最も使われているものの使用率（%）。</param>
public sealed record GpuEngineUsage(string EngineType, double UsagePercent);
