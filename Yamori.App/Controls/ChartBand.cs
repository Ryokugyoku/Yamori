using System;

namespace Yamori.Controls;

/// <summary>
/// <see cref="TimeSeriesChart"/> に色をつける区間（異常の区間）。
/// </summary>
/// <param name="StartUtc">始まり。</param>
/// <param name="EndUtc">終わり。</param>
/// <param name="IsCritical">重さが「危険」なら true（色を変える）。</param>
/// <param name="IsSelected">選んでいる区間なら true（枠を付ける）。</param>
public sealed record ChartBand(DateTimeOffset StartUtc, DateTimeOffset EndUtc, bool IsCritical, bool IsSelected);
