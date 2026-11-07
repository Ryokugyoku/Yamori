namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// 読み取りに時間がかかる、あまり変わらない値を、決めた間隔でだけ読み直す。
/// </summary>
/// <typeparam name="T">値の型。</typeparam>
/// <remarks>スレッドセーフではない。</remarks>
public sealed class PeriodicCache<T>
{
    private readonly Func<T> _load;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _lifetime;
    private T? _value;
    private DateTimeOffset? _loadedAt;

    /// <summary>
    /// 値の読み取り方と、読み直す間隔を指定して作る。
    /// </summary>
    /// <param name="load">値を読み取る処理。</param>
    /// <param name="lifetime">読み直すまでの間隔。</param>
    /// <param name="timeProvider">現在時刻の提供元。</param>
    /// <exception cref="ArgumentNullException"><paramref name="load"/> か <paramref name="timeProvider"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lifetime"/> が 0 以下。</exception>
    public PeriodicCache(Func<T> load, TimeSpan lifetime, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(load);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lifetime, TimeSpan.Zero);

        _load = load;
        _lifetime = lifetime;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// 値を取得する。前に読んでから間隔が過ぎていれば読み直す。
    /// </summary>
    /// <returns>値。</returns>
    public T Get()
    {
        var now = _timeProvider.GetUtcNow();
        if (_loadedAt is not { } loadedAt || now - loadedAt >= _lifetime)
        {
            _value = _load();
            _loadedAt = now;
        }

        return _value!;
    }
}
