namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 部品 1 つの、範囲の中の記録と集計値。
/// </summary>
/// <param name="Hardware">部品。</param>
/// <param name="Range">範囲。</param>
/// <param name="Series">重ねて描く値（負荷・温度・クロックの順。その部品にない種類は含めない）。</param>
/// <param name="TemperatureThresholdCelsius">「高温の時間」に使った温度（℃）。温度の記録がなければ null。</param>
public sealed record PartAnalysis(
    AnalysisHardware Hardware,
    AnalysisRange Range,
    IReadOnlyList<AnalysisSeries> Series,
    double? TemperatureThresholdCelsius)
{
    /// <summary>どれか 1 本でも記録があるかどうかを取得する。</summary>
    public bool HasData => Series.Any(s => s.HasData);

    /// <summary>
    /// 種類の値を取得する。
    /// </summary>
    /// <param name="kind">値の種類。</param>
    /// <returns>値。なければ null。</returns>
    public AnalysisSeries? Find(AnalysisSeriesKind kind) => Series.FirstOrDefault(s => s.Kind == kind);
}
