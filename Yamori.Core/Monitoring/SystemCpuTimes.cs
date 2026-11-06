namespace Yamori.Core.Monitoring;

/// <summary>
/// システム全体の CPU 時間の累計値。単位は 100 ナノ秒。
/// </summary>
/// <param name="Idle">アイドルだった時間。</param>
/// <param name="Kernel">カーネルモードの時間（アイドル時間を含む）。</param>
/// <param name="User">ユーザーモードの時間。</param>
public readonly record struct SystemCpuTimes(long Idle, long Kernel, long User);
