namespace Yamori.Core.Anomalies.Detection.Throttling;

/// <summary>
/// 制限がかかっていない間のクロックを、直近の一定期間だけ覚えておき、その最大を「ふだんのクロック」とする（クロックの下がり方の基準）。
/// </summary>
/// <remarks>スレッドセーフではない。</remarks>
public sealed class ClockBaseline
{
    private readonly TimeSpan _window;
    private readonly Queue<(DateTimeOffset At, double Hertz)> _samples = new();

    /// <summary>
    /// 覚えておく期間を指定して作る。
    /// </summary>
    /// <param name="window">覚えておく期間。省略時は <see cref="ThrottlingCriteria.BaselineWindow"/>。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="window"/> が 0 以下。</exception>
    public ClockBaseline(TimeSpan? window = null)
    {
        _window = window ?? ThrottlingCriteria.BaselineWindow;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_window, TimeSpan.Zero, nameof(window));
    }

    /// <summary>
    /// 制限がかかっていない間のクロックを足す。
    /// </summary>
    /// <param name="atUtc">読んだ時刻。</param>
    /// <param name="hertz">クロック（Hz）。0 以下や有限でない値は無視する。</param>
    public void Add(DateTimeOffset atUtc, double hertz)
    {
        if (double.IsFinite(hertz) && hertz > 0)
        {
            _samples.Enqueue((atUtc, hertz));
            Trim(atUtc);
        }
    }

    /// <summary>
    /// 直近の期間の最大のクロックを取得する。
    /// </summary>
    /// <param name="nowUtc">今の時刻。</param>
    /// <returns>最大のクロック（Hz）。覚えている値がなければ null。</returns>
    public double? Max(DateTimeOffset nowUtc)
    {
        Trim(nowUtc);
        return _samples.Count > 0 ? _samples.Max(s => s.Hertz) : null;
    }

    private void Trim(DateTimeOffset nowUtc)
    {
        while (_samples.Count > 0 && nowUtc - _samples.Peek().At > _window)
        {
            _samples.Dequeue();
        }
    }
}
