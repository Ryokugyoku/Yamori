namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 部品 1 つを、ふだんと比べた結果。
/// </summary>
/// <param name="HasEnoughHistory">記録が判定に必要な長さあったかどうか。false なら判定していない。</param>
/// <param name="Temperature">待機中の温度がふだんより高い状態が続いている期間。なければ null。</param>
/// <param name="IdleLoad">待機中の負荷がふだんより高い状態が続いている期間。なければ null。</param>
public sealed record UsualComparisonResult(bool HasEnoughHistory, ElevatedRun? Temperature, ElevatedRun? IdleLoad)
{
    /// <summary>記録が足りず判定しなかった結果を取得する。</summary>
    public static UsualComparisonResult NotEnoughHistory { get; } = new(false, null, null);
}
