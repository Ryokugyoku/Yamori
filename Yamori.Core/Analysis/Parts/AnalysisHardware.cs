using Yamori.Core.Sensors;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 分析する部品 1 つ（同じ種類の部品が複数あるとき、たとえばディスクが 2 台あるときは、それぞれ別のもの）。
/// </summary>
/// <param name="Part">部品の種類。</param>
/// <param name="HardwareId">ハードウェアの ID（センサーのキーの 2 つ目の区切り）。</param>
/// <param name="Name">表示名。</param>
public sealed record AnalysisHardware(AnalysisPart Part, string HardwareId, string Name)
{
    /// <summary>センサーのハードウェアの種類を取得する。</summary>
    public HardwareKind HardwareKind => AnalysisParts.ToHardwareKind(Part);

    /// <summary>異常の一覧で使う部品のキー（<see cref="Anomalies.AnomalyRecord.HardwareKey"/> と同じ形。例：<c>cpu/0</c>）を取得する。</summary>
    public string HardwareKey => SensorKeyTokens.ToToken(HardwareKind) + SensorKeyTokens.Separator + HardwareId;

    /// <summary>
    /// 部品のキー（<c>{hardwareKind}/{hardwareId}</c>）を読み取る。
    /// </summary>
    /// <param name="hardwareKey">部品のキー。</param>
    /// <param name="kind">ハードウェアの種類。</param>
    /// <param name="hardwareId">ハードウェアの ID。</param>
    /// <returns>読み取れたら true。</returns>
    public static bool TryParseKey(string? hardwareKey, out HardwareKind kind, out string hardwareId)
    {
        kind = default;
        hardwareId = string.Empty;
        var parts = hardwareKey?.Split(SensorKeyTokens.Separator);
        if (parts is not { Length: 2 }
            || !SensorKeyTokens.TryParseHardwareKind(parts[0], out kind)
            || !SensorKeyTokens.IsNormalizedSegment(parts[1]))
        {
            return false;
        }

        hardwareId = parts[1];
        return true;
    }
}
