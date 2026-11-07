namespace Yamori.Core.Sensors.Display;

/// <summary>
/// センサーごとに、表示するかどうかをヒステリシス付きで決める（ちらつきを防ぐ）。
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>カタログにないキーと <see cref="SensorStatus.Unsupported"/> は、すぐに表示しない。</item>
/// <item>読めない状態（<see cref="SensorStatus.TemporarilyUnavailable"/>、<see cref="SensorStatus.Error"/>）は、
/// <see cref="HideAfterFailures"/> 回続くまでは枠を残して「—」にし、続いたら表示しない。</item>
/// <item>表示しなくなったセンサーは、<see cref="ShowAfterSuccesses"/> 回続けて読めたら、また表示する。</item>
/// </list>
/// スレッドセーフではない（UI スレッドから使う）。
/// </remarks>
public sealed class SensorVisibilityTracker
{
    /// <summary>表示しなくなるまでの、続けて読めなかった回数の既定値。</summary>
    public const int DefaultHideAfterFailures = 3;

    /// <summary>また表示するまでの、続けて読めた回数の既定値。</summary>
    public const int DefaultShowAfterSuccesses = 2;

    private readonly Dictionary<SensorKey, Entry> _entries = [];

    /// <summary>
    /// 回数を指定して作る。
    /// </summary>
    /// <param name="hideAfterFailures">表示しなくなるまでの、続けて読めなかった回数。</param>
    /// <param name="showAfterSuccesses">また表示するまでの、続けて読めた回数。</param>
    /// <exception cref="ArgumentOutOfRangeException">回数が 1 未満。</exception>
    public SensorVisibilityTracker(int hideAfterFailures = DefaultHideAfterFailures, int showAfterSuccesses = DefaultShowAfterSuccesses)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(hideAfterFailures, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(showAfterSuccesses, 1);
        HideAfterFailures = hideAfterFailures;
        ShowAfterSuccesses = showAfterSuccesses;
    }

    /// <summary>表示しなくなるまでの、続けて読めなかった回数を取得する。</summary>
    public int HideAfterFailures { get; }

    /// <summary>また表示するまでの、続けて読めた回数を取得する。</summary>
    public int ShowAfterSuccesses { get; }

    /// <summary>
    /// カタログを反映する。新しいキーは、最初の値を読むまで「—」で表示する。カタログから消えたキーは忘れる。
    /// </summary>
    /// <param name="catalogKeys">カタログにあるキー。</param>
    /// <exception cref="ArgumentNullException"><paramref name="catalogKeys"/> が null。</exception>
    public void SetCatalog(IEnumerable<SensorKey> catalogKeys)
    {
        ArgumentNullException.ThrowIfNull(catalogKeys);

        var keys = catalogKeys.ToHashSet();
        foreach (var key in _entries.Keys.Where(k => !keys.Contains(k)).ToArray())
        {
            _entries.Remove(key);
        }

        foreach (var key in keys)
        {
            _entries.TryAdd(key, new Entry());
        }
    }

    /// <summary>
    /// 読んだ結果を反映する。
    /// </summary>
    /// <param name="reading">読んだ結果。カタログにないキーは無視する。</param>
    /// <exception cref="ArgumentNullException"><paramref name="reading"/> が null。</exception>
    public void Apply(SensorReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        if (!_entries.TryGetValue(reading.Key, out var entry))
        {
            return;
        }

        switch (reading.Status)
        {
            case SensorStatus.Ok:
                entry.Failures = 0;
                entry.Successes++;
                if (entry.Hidden && entry.Successes >= ShowAfterSuccesses)
                {
                    entry.Hidden = false;
                }

                entry.HasValue = true;
                break;

            case SensorStatus.Unsupported:
                entry.Hidden = true;
                entry.Successes = 0;
                entry.HasValue = false;
                break;

            default:
                entry.Successes = 0;
                entry.Failures++;
                entry.HasValue = false;
                if (entry.Failures >= HideAfterFailures)
                {
                    entry.Hidden = true;
                }

                break;
        }
    }

    /// <summary>
    /// 表示の状態を取得する。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <returns>表示の状態。</returns>
    public SensorDisplayState GetState(SensorKey key) =>
        !_entries.TryGetValue(key, out var entry) || entry.Hidden ? SensorDisplayState.Hidden
        : entry.HasValue ? SensorDisplayState.Value
        : SensorDisplayState.Unavailable;

    /// <summary>
    /// 表示するキー（値か「—」を表示するもの）を取得する。
    /// </summary>
    /// <returns>表示するキー。</returns>
    public IReadOnlyList<SensorKey> VisibleKeys() =>
        _entries.Where(e => !e.Value.Hidden).Select(e => e.Key).ToArray();

    /// <summary>キー 1 つ分の状態。</summary>
    private sealed class Entry
    {
        /// <summary>表示しないかどうか。</summary>
        public bool Hidden { get; set; }

        /// <summary>最後に読んだときに値があったかどうか。</summary>
        public bool HasValue { get; set; }

        /// <summary>続けて読めなかった回数。</summary>
        public int Failures { get; set; }

        /// <summary>続けて読めた回数。</summary>
        public int Successes { get; set; }
    }
}
