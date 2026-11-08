namespace Yamori.Core.Anomalies.Detection;

/// <summary>
/// 異常の種類 1 つの判定の規則。しきい値と続いた時間で判断し（機械学習は使わない）、判断の理由を数値で返す。
/// </summary>
/// <remarks>
/// <para>規則は <see cref="AnomalyDetector"/> から毎秒 1 回、同じスレッドで呼ばれる。続いた時間などの状態は規則の中に持ってよい。</para>
/// <para>新しい種類の異常は、この形の規則を足して <see cref="AnomalyDetector"/> に渡す。</para>
/// </remarks>
public interface IAnomalyRule
{
    /// <summary>この規則が判定する異常の種類を取得する。</summary>
    AnomalyKind Kind { get; }

    /// <summary>
    /// 今の値で判定する。
    /// </summary>
    /// <param name="frame">判定の材料。</param>
    /// <returns>今異常が起きている部品ごとの判断（部品ごとに 1 つまで）。起きていなければ空。</returns>
    IReadOnlyList<AnomalyFinding> Evaluate(AnomalyFrame frame);
}
