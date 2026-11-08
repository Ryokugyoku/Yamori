namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// Intel の CPU の MSR から、パッケージとコアの温度・TjMax・制限の印（熱・電力・電流など）・RAPL の電力・APERF/MPERF の実クロックを宣言して読む。
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

    /// <summary>CPU が報告している TjMax（熱で性能を落とし始める上限の温度）のキー。</summary>
    public static readonly SensorKey TjMaxKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Temperature, "tjmax");

    /// <summary>熱制御（TCC）が働き始める温度（TjMax から作動温度のずれを引いたもの）のキー。</summary>
    public static readonly SensorKey ThrottleTemperatureKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Temperature, "throttle-target");

    /// <summary>パッケージが熱で性能を落としているかの印のキー。</summary>
    public static readonly SensorKey PackageThermalThrottlingKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Flag, "thermal-throttling");

    /// <summary>ほかの部品が PROCHOT# で性能を落とさせているかの印のキー。</summary>
    public static readonly SensorKey ProchotKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Flag, "prochot");

    /// <summary>パッケージが電力の上限で性能を落としているかの印のキー。</summary>
    public static readonly SensorKey PackagePowerLimitKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Flag, "power-limit");

    /// <summary>パッケージが危険な温度に達しているかの印のキー。</summary>
    public static readonly SensorKey CriticalTemperatureKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Flag, "critical-temperature");

    /// <summary>どれかのコアが熱で性能を落としているかの印のキー。</summary>
    public static readonly SensorKey CoresThermalThrottlingKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Flag, "cores-thermal-throttling");

    /// <summary>どれかのコアが電力の上限で性能を落としているかの印のキー。</summary>
    public static readonly SensorKey CoresPowerLimitKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Flag, "cores-power-limit");

    /// <summary>どれかのコアが電流の上限で性能を落としているかの印のキー。</summary>
    public static readonly SensorKey CoresCurrentLimitKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Flag, "cores-current-limit");

    /// <summary>どれかのコアがほかのドメインの制限につられて性能を落としているかの印のキー。</summary>
    public static readonly SensorKey CoresCrossDomainLimitKey = SensorKey.Create(HardwareKind.Cpu, CpuId, SensorType.Flag, "cores-cross-domain-limit");

    private static readonly (SensorKey Key, string Name, IntelThermalLimits Limit)[] PackageFlags =
    [
        (PackageThermalThrottlingKey, "Thermal Throttling (package)", IntelThermalLimits.Thermal),
        (ProchotKey, "PROCHOT", IntelThermalLimits.Prochot),
        (PackagePowerLimitKey, "Power Limit (package)", IntelThermalLimits.PowerLimit),
        (CriticalTemperatureKey, "Critical Temperature", IntelThermalLimits.CriticalTemperature),
    ];

    private static readonly (SensorKey Key, string Name, IntelThermalLimits Limit)[] CoreFlags =
    [
        (CoresThermalThrottlingKey, "Thermal Throttling (any core)", IntelThermalLimits.Thermal),
        (CoresPowerLimitKey, "Power Limit (any core)", IntelThermalLimits.PowerLimit),
        (CoresCurrentLimitKey, "Current Limit (any core)", IntelThermalLimits.CurrentLimit),
        (CoresCrossDomainLimitKey, "Cross-Domain Limit (any core)", IntelThermalLimits.CrossDomainLimit),
    ];

    private readonly IMsrReader _reader;
    private readonly IReadOnlyList<CpuCore> _cores;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<SensorKey, CpuCore> _coreTemperatureKeys = [];
    private readonly Dictionary<SensorKey, CpuCore> _coreClockKeys = [];
    private readonly Dictionary<int, double> _tjMaxByCore = [];
    private readonly Dictionary<int, EffectiveClockMeter> _clockMeters = [];
    private readonly Dictionary<SensorKey, (uint Register, RaplEnergyMeter Meter)> _energy = [];
    private readonly List<CpuCore> _thermalCores = [];
    private double _packageTjMax = IntelMsrDecoder.FallbackTjMax;
    private double? _reportedTjMax;
    private double? _throttleTemperature;
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
        _thermalCores.Clear();
        _hasPackageTemperature = false;
        _reportedTjMax = null;
        _throttleTemperature = null;

        var descriptors = new List<SensorDescriptor>();
        var packageCore = _cores.Count > 0 ? _cores[0] : null;
        Span<ulong> two = stackalloc ulong[2];
        Span<ulong> one = stackalloc ulong[1];

        if (_reader.TryRead(packageCore, [IntelMsrRegisters.TemperatureTarget, IntelMsrRegisters.PackageThermStatus], two))
        {
            _packageTjMax = IntelMsrDecoder.TjMax(two[0]);
            _reportedTjMax = IntelMsrDecoder.ReportedTjMax(two[0]);
            _throttleTemperature = IntelMsrDecoder.ThrottleTemperature(two[0]);
            _hasPackageTemperature = true;
            descriptors.Add(Describe(PackageTemperatureKey, "CPU Package", CpuHardwareName, "temperature", providerId));
            if (_reportedTjMax is not null)
            {
                descriptors.Add(Describe(TjMaxKey, "TjMax", CpuHardwareName, "temperature", providerId));
                descriptors.Add(Describe(ThrottleTemperatureKey, "Throttle Temperature (TCC)", CpuHardwareName, "temperature", providerId));
            }

            descriptors.AddRange(PackageFlags.Select(f => Describe(f.Key, f.Name, CpuHardwareName, SensorGroups.Throttling, providerId)));
        }

        DiscoverEnergy(packageCore, providerId, descriptors);

        var baseClock = _reader.TryRead(packageCore, [IntelMsrRegisters.PlatformInfo], one) ? IntelMsrDecoder.BaseClockHertz(one[0]) : null;
        foreach (var core in _cores)
        {
            if (_reader.TryRead(core, [IntelMsrRegisters.TemperatureTarget, IntelMsrRegisters.ThermStatus], two)
                && IntelMsrDecoder.CoreTemperature(two[1], IntelMsrDecoder.TjMax(two[0])) is not null)
            {
                _tjMaxByCore[core.Index] = IntelMsrDecoder.TjMax(two[0]);
                _thermalCores.Add(core);
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

        if (_thermalCores.Count > 0)
        {
            descriptors.AddRange(CoreFlags.Select(f => Describe(f.Key, f.Name, CpuHardwareName, SensorGroups.Throttling, providerId)));
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

        if (_hasPackageTemperature)
        {
            ReadPackageThermal(packageCore, requested, values, one);
        }

        foreach (var (key, (register, meter)) in _energy)
        {
            if (requested.Contains(key))
            {
                values[key] = _reader.TryRead(packageCore, [register], one) ? meter.Update(one[0], timestamp, _timeProvider.TimestampFrequency) : null;
            }
        }

        if (_coreTemperatureKeys.Keys.Any(requested.Contains) || CoreFlags.Any(f => requested.Contains(f.Key)))
        {
            ReadCoreThermal(requested, values, one);
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

    private void ReadPackageThermal(CpuCore? packageCore, HashSet<SensorKey> requested, Dictionary<SensorKey, double?> values, Span<ulong> one)
    {
        if (requested.Contains(TjMaxKey) && _reportedTjMax is { } tjMax)
        {
            values[TjMaxKey] = tjMax;
        }

        if (requested.Contains(ThrottleTemperatureKey) && _throttleTemperature is { } throttle)
        {
            values[ThrottleTemperatureKey] = throttle;
        }

        if (!requested.Contains(PackageTemperatureKey) && !PackageFlags.Any(f => requested.Contains(f.Key)))
        {
            return;
        }

        ulong? status = _reader.TryRead(packageCore, [IntelMsrRegisters.PackageThermStatus], one) ? one[0] : null;
        if (requested.Contains(PackageTemperatureKey))
        {
            values[PackageTemperatureKey] = status is { } s ? IntelMsrDecoder.PackageTemperature(s, _packageTjMax) : null;
        }

        var limits = status is { } raw ? IntelMsrDecoder.ThermalLimits(raw) : (IntelThermalLimits?)null;
        foreach (var flag in PackageFlags)
        {
            if (requested.Contains(flag.Key))
            {
                values[flag.Key] = ToFlag(limits, flag.Limit);
            }
        }
    }

    private void ReadCoreThermal(HashSet<SensorKey> requested, Dictionary<SensorKey, double?> values, Span<ulong> one)
    {
        var statusByCore = new Dictionary<int, ulong>();
        foreach (var core in _thermalCores)
        {
            if (_reader.TryRead(core, [IntelMsrRegisters.ThermStatus], one))
            {
                statusByCore[core.Index] = one[0];
            }
        }

        foreach (var (key, core) in _coreTemperatureKeys)
        {
            if (requested.Contains(key))
            {
                values[key] = statusByCore.TryGetValue(core.Index, out var status)
                    ? IntelMsrDecoder.CoreTemperature(status, _tjMaxByCore[core.Index])
                    : null;
            }
        }

        // 1 つもコアを読めなかったときは、制限の有無は分からない。
        IntelThermalLimits? anyCore = statusByCore.Count == 0
            ? null
            : statusByCore.Values.Aggregate(IntelThermalLimits.None, (all, status) => all | IntelMsrDecoder.ThermalLimits(status));
        foreach (var flag in CoreFlags)
        {
            if (requested.Contains(flag.Key))
            {
                values[flag.Key] = ToFlag(anyCore, flag.Limit);
            }
        }
    }

    private static double? ToFlag(IntelThermalLimits? limits, IntelThermalLimits limit) =>
        limits is { } l ? ((l & limit) != 0 ? 1 : 0) : null;

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
        || (key == TjMaxKey && _reportedTjMax is not null)
        || (key == ThrottleTemperatureKey && _throttleTemperature is not null)
        || (_hasPackageTemperature && PackageFlags.Any(f => f.Key == key))
        || (_thermalCores.Count > 0 && CoreFlags.Any(f => f.Key == key))
        || _energy.ContainsKey(key)
        || _coreTemperatureKeys.ContainsKey(key)
        || _coreClockKeys.ContainsKey(key)
        || (key == AverageEffectiveClockKey && _clockMeters.Count > 0);
}
