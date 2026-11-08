using System.Collections.Generic;
using Yamori.Core.Analysis.Parts;

namespace Yamori.Controls;

/// <summary>
/// <see cref="TimeSeriesChart"/> に描く内容一式。まとめて渡し、まとめて描き直す（途中の状態を描かないため）。
/// </summary>
/// <param name="Range">時間の軸の範囲。</param>
/// <param name="Series">重ねて描く系列。</param>
/// <param name="Bands">色をつける区間。</param>
/// <param name="ReferenceLine">点線で引く目安の値。なければ null。</param>
public sealed record ChartData(
    AnalysisRange Range,
    IReadOnlyList<ChartSeries> Series,
    IReadOnlyList<ChartBand> Bands,
    ChartReferenceLine? ReferenceLine);
