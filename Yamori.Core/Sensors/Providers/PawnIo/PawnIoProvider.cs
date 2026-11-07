using System.Runtime.Intrinsics.X86;
using Yamori.Core.Sensors.Extended;

namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// PawnIO を使って、Intel の CPU の温度・電力・実クロックを MSR から読む Provider。Helper のプロセスでだけ登録する。
/// </summary>
/// <remarks>
/// <para>ハードウェアには読み取りしか行わない（MSR への書き込み、ファンの制御、オーバークロックはしない）。</para>
/// <para>PawnIO を開くのは、<see cref="IExtendedSensorControl.CanUsePawnIo"/> が true（入っている・公式版・同意済み）のときだけ。
/// 同意が取り消されたら、次の宣言で PawnIO を閉じて何も宣言しない。</para>
/// <para>PawnIO を開けない、またはモジュールを読み込めない場合は、宣言で例外を投げる（状態は <see cref="ExtendedSensorState.Unavailable"/> になる）。</para>
/// </remarks>
public sealed class PawnIoProvider : ISensorProvider, IDisposable
{
    private readonly IExtendedSensorControl _control;
    private readonly Func<IMsrReader> _openReader;
    private readonly Func<IReadOnlyList<CpuCore>> _readCores;
    private readonly Func<bool> _isSupportedCpu;
    private readonly TimeProvider _timeProvider;
    private readonly Lock _gate = new();
    private IMsrReader? _reader;
    private IntelMsrSensorSampler? _sampler;

    /// <summary>
    /// 部品を指定して作る。
    /// </summary>
    /// <param name="control">PawnIO を使ってよいかどうかの判断。</param>
    /// <param name="openReader">PawnIO を開いて MSR の読み取り器を作る処理（<see cref="PawnIoMsrReader.Open"/>）。</param>
    /// <param name="readCores">物理コアの並びを読む処理（<see cref="Win32CpuCoreLayoutSource.Read"/>）。</param>
    /// <param name="isSupportedCpu">対応している CPU（Intel）かどうかを調べる処理。省略時は CPUID で調べる。</param>
    /// <param name="timeProvider">時刻の提供元。</param>
    /// <exception cref="ArgumentNullException"><paramref name="isSupportedCpu"/> 以外の引数が null。</exception>
    public PawnIoProvider(
        IExtendedSensorControl control,
        Func<IMsrReader> openReader,
        Func<IReadOnlyList<CpuCore>> readCores,
        Func<bool>? isSupportedCpu,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(openReader);
        ArgumentNullException.ThrowIfNull(readCores);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _control = control;
        _openReader = openReader;
        _readCores = readCores;
        _isSupportedCpu = isSupportedCpu ?? IsIntelCpu;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public string ProviderId => SensorProviderIds.PawnIo;

    /// <summary>
    /// 今の CPU が Intel かどうかを CPUID で調べる。
    /// </summary>
    /// <returns>Intel（<c>GenuineIntel</c>）なら true。</returns>
    public static bool IsIntelCpu()
    {
        if (!X86Base.IsSupported)
        {
            return false;
        }

        var (_, ebx, ecx, edx) = X86Base.CpuId(0, 0);
        return ebx == 0x756E6547 && edx == 0x49656E69 && ecx == 0x6C65746E;
    }

    /// <inheritdoc />
    /// <exception cref="System.Runtime.InteropServices.COMException">PawnIO を開けない、またはモジュールを読み込めない。</exception>
    /// <exception cref="DllNotFoundException">PawnIO のライブラリがない。</exception>
    public Task<IReadOnlyList<SensorDescriptor>> DiscoverAsync(CancellationToken cancellationToken) =>
        Task.Run<IReadOnlyList<SensorDescriptor>>(
            () =>
            {
                lock (_gate)
                {
                    if (!_control.CanUsePawnIo || !_isSupportedCpu())
                    {
                        CloseLocked();
                        return [];
                    }

                    if (_reader is null)
                    {
                        _reader = _openReader();
                        _sampler = new IntelMsrSensorSampler(_reader, _readCores(), _timeProvider);
                    }

                    return _sampler!.Discover(ProviderId);
                }
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<SensorReading>> ReadAsync(IReadOnlyCollection<SensorKey> keys, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keys);
        return Task.Run(
            () =>
            {
                lock (_gate)
                {
                    if (_sampler is null || !_control.CanUsePawnIo)
                    {
                        var now = _timeProvider.GetUtcNow();
                        return (IReadOnlyList<SensorReading>)keys
                            .Select(k => SensorReading.Failed(k, SensorStatus.TemporarilyUnavailable, now, ProviderId))
                            .ToArray();
                    }

                    return _sampler.Read(keys, ProviderId);
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// PawnIO を閉じる。
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            CloseLocked();
        }
    }

    private void CloseLocked()
    {
        _sampler = null;
        _reader?.Dispose();
        _reader = null;
    }
}
