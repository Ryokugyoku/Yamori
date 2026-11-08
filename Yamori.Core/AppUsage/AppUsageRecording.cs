using Microsoft.Data.Sqlite;
using Yamori.Core.AppUsage.Recording;
using Yamori.Core.AppUsage.Sampling;
using Yamori.Core.AppUsage.Service;
using Yamori.Core.AppUsage.Storage;
using Yamori.Core.Realtime.Counters;

namespace Yamori.Core.AppUsage;

/// <summary>
/// Helper の中で、アプリごとの CPU 使用率の記録（測定・保存・読み出しの窓口）をまとめて持つ。
/// </summary>
/// <remarks>記録のファイルを開けなかった場合は、記録せずに「記録なし」を返す窓口だけを持つ。</remarks>
public sealed class AppUsageRecording : IDisposable
{
    /// <summary>記録のファイルの名前（Helper のデータフォルダーに置く）。</summary>
    public const string FileName = "app-usage.db";

    private readonly SqliteAppUsageStore? _store;
    private readonly CounterAppCpuSampler? _sampler;
    private readonly AppCpuRecorder? _recorder;

    private AppUsageRecording(SqliteAppUsageStore? store, CounterAppCpuSampler? sampler, AppCpuRecorder? recorder)
    {
        _store = store;
        _sampler = sampler;
        _recorder = recorder;
        Service = new LocalAppUsageService(store);
    }

    /// <summary>読み出しの窓口を取得する。</summary>
    public IAppUsageService Service { get; }

    /// <summary>記録が使えるかどうかを取得する。</summary>
    public bool IsAvailable => _recorder is not null;

    /// <summary>
    /// Helper の構成で作る。データフォルダーに記録のファイルを開き、パフォーマンスカウンターで測る。
    /// </summary>
    /// <param name="dataDirectory">Helper のデータフォルダー。</param>
    /// <param name="timeProvider">現在時刻とタイマーの提供元。</param>
    /// <returns>新しいインスタンス。使い終わったら破棄すること。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static AppUsageRecording CreateForHelper(string dataDirectory, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(dataDirectory);
        ArgumentNullException.ThrowIfNull(timeProvider);

        SqliteAppUsageStore? store = null;
        CounterAppCpuSampler? sampler = null;
        try
        {
            store = SqliteAppUsageStore.Open(Path.Combine(dataDirectory, FileName));
            sampler = new CounterAppCpuSampler(new PdhPerformanceCounterSampler(), Environment.ProcessorCount);
            return new AppUsageRecording(store, sampler, new AppCpuRecorder(sampler, store, timeProvider));
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException or DllNotFoundException)
        {
            // ほかのプロセスが開いている、ディスクに書けない、カウンターを開けないなど。記録なしで動く。
            sampler?.Dispose();
            store?.Dispose();
            return new AppUsageRecording(null, null, null);
        }
    }

    /// <summary>
    /// キャンセルされるまで記録を続ける。記録が使えない場合はすぐに完了する。
    /// </summary>
    /// <param name="cancellationToken">停止の合図。</param>
    /// <returns>停止したときに完了するタスク。</returns>
    public Task RunAsync(CancellationToken cancellationToken) =>
        _recorder is null ? Task.CompletedTask : Task.Run(() => _recorder.RunAsync(cancellationToken), CancellationToken.None);

    /// <summary>
    /// カウンターと記録のファイルを閉じる。<see cref="RunAsync"/> を止めてから呼ぶこと。
    /// </summary>
    public void Dispose()
    {
        _sampler?.Dispose();
        _store?.Dispose();
    }
}
