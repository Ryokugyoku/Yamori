using Yamori.Core.Hardware;

namespace Yamori.Core.Recommendations;

/// <summary>
/// 1 つの部品について、次の PC のおすすめを判定する方針。
/// </summary>
public interface IRecommendationPolicy
{
    /// <summary>
    /// 判定する部品の種類を取得する。
    /// </summary>
    HardwareComponent Component { get; }

    /// <summary>
    /// おすすめを判定する。
    /// </summary>
    /// <param name="input">判定の材料。</param>
    /// <returns>判定結果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> が null。</exception>
    HardwareRecommendation Evaluate(RecommendationInput input);
}
