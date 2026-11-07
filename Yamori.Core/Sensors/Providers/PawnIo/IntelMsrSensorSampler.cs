namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// Intel の CPU の MSR から、パッケージとコアの温度・RAPL の電力・APERF/MPERF の実クロックを宣言して読む。
/// </summary>
/// <remarks>
/// <para>宣言では、実際に読めた MSR のセンサーだけを宣言する。差分で求める値（電力・実クロック）は、宣言の直後の読み取りでは
/// <see cref="SensorStatus.TemporarilyUnavailable"/> になる。</para>
/// <para>スレッドセーフではない。呼び出し側で 1 つずつ呼ぶこと。</para>
/// </remarks>
public sealed class IntelMsrSensorSampler
{
    /// <summary>コアごとの値のグループ（初期設定では時系列に記録しない）。</summary>
    public const string CoresGroup = "cores";

    private const string CpuHardwareName = "CPU";
    private const string MemoryHardwareName = "Memory";

    private static readonly string CpuId = HardwareIds.FromSocket(0);

    /// <summary>CPU パッケージの温度のキー。</summary>
    public static readonly SensorKey PackageTemperatureKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Temperature, "package");

    /// <summary>CPU パッケージの電力のキー（標準 API の RAPL と同じ意味のため同じキー）。</summary>
    public static readonly SensorKey PackagePowerKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Power, "package");

    /// <summary>CPU のコア部分（PP0）の電力のキー。</summary>
    public static readonly SensorKey CoresPowerKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Power, "cores");

    /// <summary>DRAM の電力のキー。</summary>
    public static readonly SensorKey DramPowerKey = SensorKey.Create(HardwareKind.Memory, HardwareIds.Singleton, SensorType.Power, "dram");

    /// <summary>全コアの平均の実クロックのキー。</summary>
    public static readonly SensorKey AverageEffectiveClockKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Clock, "average-effective");

    private readonly IMsrReader _reader;
    private readonly IReadOnlyList<CpuCore> _cores;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<SensorKey, CpuCore> _coreTemperatureKeys = [];
    private readonly Dictionary<SensorKey, CpuCore> _coreClockKeys = [];
    private readonly Dictionary<int, double> _tjMaxByCore = [];
    private readonly Dictionary<int, EffectiveClockMeter> _clockMeters = [];
    private readonly Dictionary<SensorKey, (uint Register, RaplEnergyMeter Meter)> _energy = [];
    private double _packageTjMax = IntelMsrDecoder.FallbackTjMax;
    private bool _hasPackageTemperature;

    /// <summary>
    /// MSR の読み方とコアの並びを指定して作る。
    /// </summary>
    /// <param name="reader">MSR の読み取り器。</param>
    /// <param name="cores">物理コアの一覧（<see cref="Win32CpuCoreLayoutSource.Read"/>）。</param>
    /// <param name="timeProvider">時刻の提供元（電力の差分の時間に使う）。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public IntelMsrSensorSampler(IMsrReader reader, IReadOnlyList<CpuCore> cores, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(cores);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _reader = reader;
        _cores = cores;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// 読めるセンサーを調べて宣言する。前の宣言の状態（差分の基準）は捨てる。
    /// </summary>
    /// <param name="providerId">宣言する Provider の ID。</param>
    /// <returns>読めるセンサーの説明。</returns>
    public IReadOnlyList<SensorDescriptor> Discover(string providerId)
    {
        ArgumentNullException.ThrowIfNull(providerId);

        _coreTemperatureKeys.Clear();
        _coreClockKeys.Clear();
        _tjMaxByCore.Clear();
        _clockMeters.Clear();
        _energy.Clear();
        _hasPackageTemperature = false;

        var descriptors = new List<SensorDescriptor>();
        var packageCore = _cores.Count > 0 ? _cores[0] : null;
        Span<ulong> two = stackalloc ulong[2];
        Span<ulong> one = stackalloc ulong[1];

        if (_reader.TryRead(packageCore, [IntelMsrRegisters.TemperatureTarget, IntelMsrRegisters.PackageThermStatus], two))
        {
            _packageTjMax = IntelMsrDecoder.TjMax(two[0]);
            _hasPackageTemperature = true;
            descriptors.Add(Describe(PackageTemperatureKey, "CPU Package", CpuHardwareName, "temperature", providerId));
        }

        DiscoverEnergy(packageCore, providerId, descriptors);

        var baseClock = _reader.TryRead(packageCore, [IntelMsrRegisters.PlatformInfo], one) ? IntelMsrDecoder.BaseClockHertz(one[0]) : null;
        foreach (var core in _cores)
        {
            if (_reader.TryRead(core, [IntelMsrRegisters.TemperatureTarget, IntelMsrRegisters.ThermStatus], two)
                && IntelMsrDecoder.CoreTemperature(two[1], IntelMsrDecoder.TjMax(two[0])) is not null)
            {
                _tjMaxByCore[core.Index] = IntelMsrDecoder.TjMax(two[0]);
                var key = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Temperature, core.KeyName);
                _coreTemperatureKeys[key] = core;
                descriptors.Add(Describe(key, core.DisplayName, CpuHardwareName, CoresGroup, providerId));
            }

            if (baseClock is { } clock && _reader.TryRead(core, [IntelMsrRegisters.Aperf, IntelMsrRegisters.Mperf], two))
            {
                var meter = new EffectiveClockMeter(clock);
                meter.Update(two[0], two[1]);
                _clockMeters[core.Index] = meter;
                var key = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Clock, core.KeyName + "-effective");
                _coreClockKeys[key] = core;
                descriptors.Add(Describe(key, core.DisplayName + " Effective Clock", CpuHardwareName, CoresGroup, providerId));
            }
        }

        if (_clockMeters.Count > 0)
        {
            descriptors.Add(Describe(AverageEffectiveClockKey, "Average Effective Clock", CpuHardwareName, "clock", providerId));
        }

        return descriptors;
    }

    /// <summary>
    /// 指定したセンサーの今の値を読む。
    /// </summary>
    /// <param name="keys">読むセンサーのキー。</param>
    /// <param name="providerId">読んだ Provider の ID。</param>
    /// <returns>キーごとの結果。宣言していないキーは <see cref="SensorStatus.Unsupported"/>。</returns>
    public IReadOnlyList<SensorReading> Read(IReadOnlyCollection<SensorKey> keys, string providerId)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(providerId);

        var now = _timeProvider.GetUtcNow();
        var timestamp = _timeProvider.GetTimestamp();
        var requested = keys.ToHashSet();
        var values = new Dictionary<SensorKey, double?>();
        var packageCore = _cores.Count > 0 ? _cores[0] : null;
        Span<ulong> one = stackalloc ulong[1];
        Span<ulong> two = stackalloc ulong[2];

        if (_hasPackageTemperature && requested.Contains(PackageTemperatureKey))
        {
            values[PackageTemperatureKey] = _reader.TryRead(packageCore, [IntelMsrRegisters.PackageThermStatus], one)
                ? IntelMsrDecoder.PackageTemperature(one[0], _packageTjMax)
                : null;
        }

        foreach (var (key, (register, meter)) in _energy)
        {
            if (requested.Contains(key))
            {
                values[key] = _reader.TryRead(packageCore, [register], one) ? meter.Update(one[0], timestamp, _timeProvider.TimestampFrequency) : null;
            }
        }

        foreach (var (key, core) in _coreTemperatureKeys)
        {
            if (requested.Contains(key))
            {
                values[key] = _reader.TryRead(core, [IntelMsrRegisters.ThermStatus], one)
                    ? IntelMsrDecoder.CoreTemperature(one[0], _tjMaxByCore[core.Index])
                    : null;
            }
        }

        if (_coreClockKeys.Keys.Any(requested.Contains) || requested.Contains(AverageEffectiveClockKey))
        {
            ReadClocks(requested, values, two);
        }

        return keys
            .Select(k => values.TryGetValue(k, out var value)
                ? value is { } v && double.IsFinite(v)
                    ? SensorReading.Ok(k, v, now, providerId)
                    : SensorReading.Failed(k, SensorStatus.TemporarilyUnavailable, now, providerId)
                : SensorReading.Failed(k, IsDeclared(k) ? SensorStatus.TemporarilyUnavailable : SensorStatus.Unsupported, now, providerId))
            .ToArray();
    }

    private static SensorDescriptor Describe(SensorKey key, string displayName, string hardwareName, string group, string providerId) =>
        new(key, SensorUnits.ForType(key.SensorType), displayName, hardwareName, group, providerId);

    private void DiscoverEnergy(CpuCore? packageCore, string providerId, List<SensorDescriptor> descriptors)
    {
        Span<ulong> one = stackalloc ulong[1];
        if (!_reader.TryRead(packageCore, [IntelMsrRegisters.RaplPowerUnit], one) || ((one[0] >> 8) & 0x1F) == 0)
        {
            return;
        }

        var joulesPerCount = IntelMsrDecoder.JoulesPerCount(one[0]);
        var timestamp = _timeProvider.GetTimestamp();
        var domains = new (SensorKey Key, uint Register, string Name, string Hardware, bool RequireNonZero)[]
        {
            (PackagePowerKey, IntelMsrRegisters.PackageEnergyStatus, "Package", CpuHardwareName, false),
            (CoresPowerKey, IntelMsrRegisters.Pp0EnergyStatus, "Cores (PP0)", CpuHardwareName, true),
            (DramPowerKey, IntelMsrRegisters.DramEnergyStatus, "DRAM", MemoryHardwareName, true),
        };

        foreach (var domain in domains)
        {
            // 対応していない領域のカウンターは 0 のまま進まない。
            if (_reader.TryRead(packageCore, [domain.Register], one) && (!domain.RequireNonZero || (uint)one[0] != 0))
            {
                var meter = new RaplEnergyMeter(joulesPerCount);
                meter.Update(one[0], timestamp, _timeProvider.TimestampFrequency);
                _energy[domain.Key] = (domain.Register, meter);
                descriptors.Add(Describe(domain.Key, domain.Name, domain.Hardware, "power", providerId));
            }
        }
    }

    private void ReadClocks(HashSet<SensorKey> requested, Dictionary<SensorKey, double?> values, Span<ulong> two)
    {
        var clocks = new List<double>();
        foreach (var (key, core) in _coreClockKeys)
        {
            double? clock = _reader.TryRead(core, [IntelMsrRegisters.Aperf, IntelMsrRegisters.Mperf], two)
                ? _clockMeters[core.Index].Update(two[0], two[1])
                : null;
            if (clock is { } c)
            {
                clocks.Add(c);
            }

            if (requested.Contains(key))
            {
                values[key] = clock;
            }
        }

        if (requested.Contains(AverageEffectiveClockKey))
        {
            values[AverageEffectiveClockKey] = clocks.Count > 0 ? clocks.Average() : null;
        }
    }

    private bool IsDeclared(SensorKey key) =>
        (key == PackageTemperatureKey && _hasPackageTemperature)
        || _energy.ContainsKey(key)
        || _coreTemperatureKeys.ContainsKey(key)
        || _coreClockKeys.ContainsKey(key)
        || (key == AverageEffectiveClockKey && _clockMeters.Count > 0);
}
