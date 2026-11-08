namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 「ふだんより悪い」の判定の決まりごと（比べる期間・区切りの長さ・負荷の帯・しきい値・続いた時間の下限）。
/// 初期値の根拠は PROGRESS.md の 2026-11-08 の項目に書く。
/// </summary>
/// <param name="HistoryWindow">比べる相手の期間（今から遡る長さ）。</param>
/// <param name="MinimumHistory">判定を始めるのに必要な記録の長さ。最も古い記録がこれより新しい間は判定しない。</param>
/// <param name="RecentWindow">判定する直近の期間。比べる相手（ふだん）からは除く（今の悪い状態でふだんが引き上げられないように）。</param>
/// <param name="BucketLength">判定の区切りの長さ（1 区切りごとに、ふだんより高いかを決める）。</param>
/// <param name="EvaluationInterval">判定する間隔。</param>
/// <param name="EvaluationDelay">区切りの終わりから判定するまでの待ち時間（最後の 1 分集計が書かれるのを待つ）。</param>
/// <param name="IdleLoadCeilingPercent">「待機中」とみなす負荷の上限（%、含まない）。温度はこれより低い負荷の分だけを比べる。</param>
/// <param name="LoadBandEdges">待機中の負荷を帯に分ける境目（%、昇順）。同じ帯どうしで温度を比べる。</param>
/// <param name="MinimumBaselineMinutesPerBand">帯のふだんの温度を使うのに必要な、比べる相手の期間の分の数。</param>
/// <param name="MinimumIdleMinutesPerBucket">区切りの温度を判定するのに必要な、待機中の分の数。</param>
/// <param name="MinimumMinutesPerBucket">区切りの待機中の負荷を判定するのに必要な、記録のある分の数。</param>
/// <param name="MinimumBaselineBuckets">ふだんの待機中の負荷を使うのに必要な、比べる相手の期間の区切りの数。</param>
/// <param name="IdleLoadPercentile">区切りの「待機中の負荷」とみなす、分ごとの負荷の百分位（0〜100。低いほど静かな時間の値）。</param>
/// <param name="TemperatureDeltaCelsius">温度がふだんより何℃ 高ければ「高い」とするか（手動のしきい値がないとき）。</param>
/// <param name="LoadDeltaPoints">待機中の負荷がふだんより何ポイント高ければ「高い」とするか。</param>
/// <param name="MinimumElevatedDuration">続いた時間の下限（「高い」区切りの合計）。これより短い山は無視する。</param>
/// <param name="MaxGap">判定できない区切り（待機中の時間がない・記録がない）を挟んでも続きとみなす長さ。</param>
/// <param name="WarningFactor">差がしきい値の何倍以上なら「警告」にするか。</param>
public sealed record UsualComparisonOptions(
    TimeSpan HistoryWindow,
    TimeSpan MinimumHistory,
    TimeSpan RecentWindow,
    TimeSpan BucketLength,
    TimeSpan EvaluationInterval,
    TimeSpan EvaluationDelay,
    double IdleLoadCeilingPercent,
    IReadOnlyList<double> LoadBandEdges,
    int MinimumBaselineMinutesPerBand,
    int MinimumIdleMinutesPerBucket,
    int MinimumMinutesPerBucket,
    int MinimumBaselineBuckets,
    double IdleLoadPercentile,
    double TemperatureDeltaCelsius,
    double LoadDeltaPoints,
    TimeSpan MinimumElevatedDuration,
    TimeSpan MaxGap,
    double WarningFactor)
{
    /// <summary>
    /// 初期の決まりごとを取得する。直近 14 日（判定する直近 24 時間を除く）と比べ、記録が 7 日分たまってから、1 時間ごとに判定する。
    /// 待機中は負荷 30% 未満、帯は 0〜5・5〜10・10〜20・20〜30%。温度は 8℃、待機中の負荷は 10 ポイント高い状態が合わせて 3 時間続いたら異常。
    /// </summary>
    public static UsualComparisonOptions Default { get; } = new(
        HistoryWindow: TimeSpan.FromDays(14),
        MinimumHistory: TimeSpan.FromDays(7),
        RecentWindow: TimeSpan.FromHours(24),
        BucketLength: TimeSpan.FromHours(1),
        EvaluationInterval: TimeSpan.FromHours(1),
        EvaluationDelay: TimeSpan.FromMinutes(2),
        IdleLoadCeilingPercent: 30,
        LoadBandEdges: [5, 10, 20],
        MinimumBaselineMinutesPerBand: 60,
        MinimumIdleMinutesPerBucket: 15,
        MinimumMinutesPerBucket: 30,
        MinimumBaselineBuckets: 24,
        IdleLoadPercentile: 10,
        TemperatureDeltaCelsius: 8,
        LoadDeltaPoints: 10,
        MinimumElevatedDuration: TimeSpan.FromHours(3),
        MaxGap: TimeSpan.FromHours(3),
        WarningFactor: 2);
}
