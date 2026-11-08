using Yamori.Core.Analysis.Parts;

namespace Yamori.Presentation.Analysis;

/// <summary>
/// 同じ種類の部品が複数あるとき（ディスクが 2 台など）に、分析する部品を選ぶ選択肢。
/// </summary>
/// <param name="Hardware">部品。</param>
public sealed record AnalysisHardwareOption(AnalysisHardware Hardware)
{
    /// <summary>表示名を取得する。</summary>
    public string Name => Hardware.Name;

    /// <inheritdoc />
    public override string ToString() => Name;
}
