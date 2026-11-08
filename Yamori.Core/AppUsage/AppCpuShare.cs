namespace Yamori.Core.AppUsage;

/// <summary>
/// 1 つのアプリ（同じ名前のプロセスをまとめた単位）の CPU 使用率。
/// </summary>
/// <param name="Name">アプリのプロセス名（拡張子なし、例：chrome）。</param>
/// <param name="CpuPercent">CPU 使用率（PC 全体を 100 とする %）。</param>
public readonly record struct AppCpuShare(string Name, double CpuPercent);
