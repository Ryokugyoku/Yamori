using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Detection;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// テストから「今の判断」を決められる規則。受け取った材料も覚えておく。
/// </summary>
internal sealed class ScriptedAnomalyRule(AnomalyKind kind = AnomalyKind.ThermalThrottling) : IAnomalyRule
{
    public AnomalyKind Kind { get; } = kind;

    public List<AnomalyFinding> Findings { get; } = [];

    public bool Throw { get; set; }

    public AnomalyFrame? LastFrame { get; private set; }

    public IReadOnlyList<AnomalyFinding> Evaluate(AnomalyFrame frame)
    {
        LastFrame = frame;
        if (Throw)
        {
            throw new InvalidOperationException("rule failed");
        }

        return Findings.ToArray();
    }
}
