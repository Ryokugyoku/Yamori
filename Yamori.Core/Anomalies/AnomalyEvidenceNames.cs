namespace Yamori.Core.Anomalies;

/// <summary>
/// 判断の理由の数値（<see cref="AnomalyRecord.Evidence"/>）の名前。通知の文面と画面の説明は、この名前で値を取り出して作る。
/// </summary>
/// <remarks>マスタと IPC にそのまま残るため、既存の名前は変えないこと。</remarks>
public static class AnomalyEvidenceNames
{
    /// <summary>異常の間で最も高かった温度（℃）。</summary>
    public const string PeakTemperatureCelsius = "peakTemperatureCelsius";

    /// <summary>判定に使った温度のしきい値（℃）。</summary>
    public const string ThresholdCelsius = "thresholdCelsius";

    /// <summary>部品が報告している上限の温度（TjMax など。℃）。</summary>
    public const string TjMaxCelsius = "tjMaxCelsius";

    /// <summary>ふだんのクロックからの下がり方（%）。</summary>
    public const string ClockDropPercent = "clockDropPercent";

    /// <summary>異常の間の平均の負荷（%）。</summary>
    public const string LoadPercent = "loadPercent";

    /// <summary>続いた時間（秒）。</summary>
    public const string DurationSeconds = "durationSeconds";

    /// <summary>異常の間の平均のクロック（Hz）。</summary>
    public const string ClockHertz = "clockHertz";

    /// <summary>異常が始まる前 10 分間の最も高いクロック（Hz。クロックの下がり方の基準）。</summary>
    public const string BaselineClockHertz = "baselineClockHertz";

    /// <summary>熱による制限の印（熱・PROCHOT・危険な温度・温度ゾーンの制限）がオンだった時間（秒）。</summary>
    public const string ThermalLimitSeconds = "thermalLimitSeconds";

    /// <summary>温度が注意の温度以上だった時間（秒）。</summary>
    public const string AboveThresholdSeconds = "aboveThresholdSeconds";

    /// <summary>電力の制限（電力の上限、Windows の性能の制限）がかかっていた時間（秒）。</summary>
    public const string PowerLimitSeconds = "powerLimitSeconds";

    /// <summary>電流の制限（電流の上限、ほかのドメインにつられた制限）がかかっていた時間（秒）。</summary>
    public const string CurrentLimitSeconds = "currentLimitSeconds";

    /// <summary>比べた「ふだん」の値（℃ など、異常の種類による）。</summary>
    public const string BaselineValue = "baselineValue";

    /// <summary>「ふだん」との差（℃ など、異常の種類による）。</summary>
    public const string DeltaFromBaseline = "deltaFromBaseline";
}
