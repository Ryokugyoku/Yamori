using Yamori.Core.Hardware;
using Yamori.Core.Monitoring;

namespace Yamori.Core.Storage;

/// <summary>
/// 実際の保存先を、アプリを動かしたまま差し替えられる履歴ストア。
/// </summary>
/// <remarks>
/// 監視処理やレポート処理はこのクラスだけを参照するため、保存先を変えても作り直す必要がない。
/// </remarks>
public sealed class SwitchableUsageHistoryStore : IUsageHistoryStore
{
    private readonly Lock _sync = new();
    private IUsageHistoryStore _current;

    /// <summary>
    /// 最初の保存先を指定して作る。
    /// </summary>
    /// <param name="initial">最初の保存先。</param>
    /// <exception cref="ArgumentNullException"><paramref name="initial"/> が null。</exception>
    public SwitchableUsageHistoryStore(IUsageHistoryStore initial)
    {
        ArgumentNullException.ThrowIfNull(initial);
        _current = initial;
    }

    /// <inheritdoc />
    public void Append(IReadOnlyCollection<ComponentUsageSample> samples)
    {
        lock (_sync)
        {
            _current.Append(samples);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ComponentUsageSample> Read(HardwareComponent component, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_sync)
        {
            return _current.Read(component, from, to);
        }
    }

    /// <summary>
    /// 保存先を差し替える。<paramref name="beforeSwitch"/> の実行中は読み書きを待たせるため、移行中の書き込みが失われない。
    /// </summary>
    /// <param name="next">新しい保存先。</param>
    /// <param name="beforeSwitch">差し替える直前に行う処理（履歴のコピーなど）。省略可。例外を投げたら差し替えない。</param>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> が null。</exception>
    public void SwitchTo(IUsageHistoryStore next, Action? beforeSwitch = null)
    {
        ArgumentNullException.ThrowIfNull(next);
        lock (_sync)
        {
            beforeSwitch?.Invoke();
            _current = next;
        }
    }
}
