using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 範囲の長さに応じて、読む記録（解像度）を決める。24 時間以内なら 1 秒ごとの記録、それより長ければ 1 分集計を使う。
/// </summary>
/// <remarks>
/// 1 秒ごとの記録は一部のセンサー（<see cref="FineSeriesSelection"/>）だけなので、読んで空なら 1 分集計で読み直す（<see cref="Fallback"/>）。
/// </remarks>
public static class AnalysisResolutions
{
    /// <summary>1 秒ごとの記録を使う最長の範囲（1 秒ごとの記録の保持期間）。</summary>
    public static readonly TimeSpan FineLimit = TimeSpan.FromDays(1);

    /// <summary>1 秒ごとの記録を 1 回で読む長さ（約 7,200 点、約 1 MB。パイプの 1 通の上限より十分小さくする）。</summary>
    public static readonly TimeSpan FineChunkLength = TimeSpan.FromHours(2);

    /// <summary>1 分集計を 1 回で読む長さ（約 1 万点、約 1.3 MB）。</summary>
    public static readonly TimeSpan MinuteChunkLength = TimeSpan.FromDays(7);

    /// <summary>
    /// 範囲に使う解像度を決める。
    /// </summary>
    /// <param name="range">範囲。</param>
    /// <returns>24 時間以内なら <see cref="TimeSeriesResolution.OneSecond"/>、それより長ければ <see cref="TimeSeriesResolution.OneMinute"/>。</returns>
    public static TimeSeriesResolution Choose(AnalysisRange range) =>
        range.Length <= FineLimit ? TimeSeriesResolution.OneSecond : TimeSeriesResolution.OneMinute;

    /// <summary>
    /// 記録が空だったときに読み直す解像度を取得する。
    /// </summary>
    /// <param name="resolution">読んだ解像度。</param>
    /// <returns>読み直す解像度。なければ null。</returns>
    public static TimeSeriesResolution? Fallback(TimeSeriesResolution resolution) =>
        resolution == TimeSeriesResolution.OneSecond ? TimeSeriesResolution.OneMinute : null;

    /// <summary>
    /// 1 回で読む長さを取得する。
    /// </summary>
    /// <param name="resolution">解像度。</param>
    /// <returns>1 回で読む長さ。</returns>
    public static TimeSpan ChunkLength(TimeSeriesResolution resolution) =>
        resolution == TimeSeriesResolution.OneSecond ? FineChunkLength : MinuteChunkLength;

    /// <summary>
    /// 1 点が表す時間の長さを取得する（高温の時間の合計と、記録が途切れたかの判断に使う）。
    /// </summary>
    /// <param name="resolution">解像度。</param>
    /// <returns>1 点の長さ。</returns>
    public static TimeSpan StepOf(TimeSeriesResolution resolution) => resolution switch
    {
        TimeSeriesResolution.OneSecond => TimeSpan.FromSeconds(1),
        TimeSeriesResolution.OneMinute => TimeSpan.FromMinutes(1),
        TimeSeriesResolution.OneHour => TimeSpan.FromHours(1),
        _ => DefaultSensorLogPolicies.DefaultInterval,
    };
}
