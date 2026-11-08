#if DEBUG
using System.Text.Json;
using Yamori.Core.Anomalies.Detection.Usual;

namespace Yamori.Helper;

/// <summary>
/// Debug のビルドだけで使う、「ふだんより悪い」の判定の確かめ用の決まりごと。Helper の exe と同じフォルダーに
/// <see cref="FileName"/> を置くと、起動時に読み、書かれた項目だけを初期値から差し替える（7 日分の記録を待たずに、短い期間で判定を確かめるため）。
/// </summary>
/// <remarks>
/// 書ける項目（どれも省略可）：<c>historyMinutes</c>・<c>minimumHistoryMinutes</c>・<c>recentMinutes</c>・<c>bucketMinutes</c>・
/// <c>evaluationMinutes</c>・<c>evaluationDelaySeconds</c>・<c>minimumElevatedMinutes</c>・<c>maxGapMinutes</c>・
/// <c>minimumBaselineMinutesPerBand</c>・<c>minimumIdleMinutesPerBucket</c>・<c>minimumMinutesPerBucket</c>・<c>minimumBaselineBuckets</c>・
/// <c>temperatureDeltaCelsius</c>・<c>loadDeltaPoints</c>。Release のビルドには含まれない。
/// </remarks>
public static class DebugUsualComparisonOptions
{
    /// <summary>確かめ用の決まりごとのファイルの名前。</summary>
    public const string FileName = "usual-debug.json";

    /// <summary>
    /// 確かめ用の決まりごとを読む。
    /// </summary>
    /// <param name="path">ファイルのパス。</param>
    /// <returns>差し替えた決まりごと。ファイルがないか読めなければ null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> が null。</exception>
    public static UsualComparisonOptions? Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var d = UsualComparisonOptions.Default;
            return d with
            {
                HistoryWindow = Minutes(root, "historyMinutes") ?? d.HistoryWindow,
                MinimumHistory = Minutes(root, "minimumHistoryMinutes") ?? d.MinimumHistory,
                RecentWindow = Minutes(root, "recentMinutes") ?? d.RecentWindow,
                BucketLength = Minutes(root, "bucketMinutes") ?? d.BucketLength,
                EvaluationInterval = Minutes(root, "evaluationMinutes") ?? d.EvaluationInterval,
                EvaluationDelay = Number(root, "evaluationDelaySeconds") is { } delay ? TimeSpan.FromSeconds(delay) : d.EvaluationDelay,
                MinimumElevatedDuration = Minutes(root, "minimumElevatedMinutes") ?? d.MinimumElevatedDuration,
                MaxGap = Minutes(root, "maxGapMinutes") ?? d.MaxGap,
                MinimumBaselineMinutesPerBand = (int?)Number(root, "minimumBaselineMinutesPerBand") ?? d.MinimumBaselineMinutesPerBand,
                MinimumIdleMinutesPerBucket = (int?)Number(root, "minimumIdleMinutesPerBucket") ?? d.MinimumIdleMinutesPerBucket,
                MinimumMinutesPerBucket = (int?)Number(root, "minimumMinutesPerBucket") ?? d.MinimumMinutesPerBucket,
                MinimumBaselineBuckets = (int?)Number(root, "minimumBaselineBuckets") ?? d.MinimumBaselineBuckets,
                TemperatureDeltaCelsius = Number(root, "temperatureDeltaCelsius") ?? d.TemperatureDeltaCelsius,
                LoadDeltaPoints = Number(root, "loadDeltaPoints") ?? d.LoadDeltaPoints,
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    private static double? Number(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;

    private static TimeSpan? Minutes(JsonElement root, string name) => Number(root, name) is { } minutes ? TimeSpan.FromMinutes(minutes) : null;
}
#endif
