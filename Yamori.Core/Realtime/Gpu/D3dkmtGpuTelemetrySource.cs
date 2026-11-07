using System.Globalization;
using System.Runtime.InteropServices;
using Yamori.Core.Interop;
using Yamori.Core.Realtime.Counters;

namespace Yamori.Core.Realtime.Gpu;

/// <summary>
/// グラフィックスカーネル（D3DKMT）に問い合わせて、GPU のセンサーの値を読み取る。タスク マネージャーと同じ情報源で、
/// 管理者権限やドライバーの追加は必要ない。
/// </summary>
/// <remarks>
/// 値を公開するかどうかは GPU のドライバー次第で、公開していない値は null になる。
/// 呼び出すたびにアダプターを開き直すため、GPU の追加・取り外しにも追従する。
/// </remarks>
public sealed class D3dkmtGpuTelemetrySource : IGpuTelemetrySource
{
    private const string FallbackName = "GPU";

    /// <inheritdoc />
    public IReadOnlyList<GpuTelemetry> Read()
    {
        var adapters = EnumerateAdapters();
        try
        {
            return adapters
                .Where(a => !IsSoftwareAdapter(a.Adapter))
                .Select(ReadAdapter)
                .ToArray();
        }
        finally
        {
            foreach (var adapter in adapters)
            {
                _ = NativeMethods.D3DKMTCloseAdapter(new NativeMethods.D3dkmtCloseAdapter { Adapter = adapter.Adapter });
            }
        }
    }

    private static NativeMethods.D3dkmtAdapterInfo[] EnumerateAdapters()
    {
        try
        {
            var query = default(NativeMethods.D3dkmtEnumAdapters2);
            if (NativeMethods.D3DKMTEnumAdapters2(ref query) != 0 || query.NumAdapters == 0)
            {
                return [];
            }

            var adapters = new NativeMethods.D3dkmtAdapterInfo[query.NumAdapters];
            var handle = GCHandle.Alloc(adapters, GCHandleType.Pinned);
            try
            {
                query.Adapters = handle.AddrOfPinnedObject();
                if (NativeMethods.D3DKMTEnumAdapters2(ref query) != 0)
                {
                    return [];
                }

                return adapters.AsSpan(0, (int)Math.Min(query.NumAdapters, (uint)adapters.Length)).ToArray();
            }
            finally
            {
                handle.Free();
            }
        }
        catch (EntryPointNotFoundException)
        {
            // Windows 8 より前の Windows。GPU のセンサーの値は読めない。
            return [];
        }
    }

    private static GpuTelemetry ReadAdapter(NativeMethods.D3dkmtAdapterInfo adapter)
    {
        var luid = GpuCounterInstance.ToLuid(unchecked((uint)adapter.LuidHighPart), adapter.LuidLowPart);
        var telemetry = new GpuTelemetry(luid, ReadName(adapter.Adapter) ?? FallbackName);

        if (TryRead(adapter.Adapter, NativeMethods.KmtQueryAdapterAddress, out NativeMethods.D3dkmtAdapterAddress address))
        {
            telemetry = telemetry with
            {
                PciLocation = string.Create(
                    CultureInfo.InvariantCulture,
                    $"{address.BusNumber:x2}:{address.DeviceNumber:x2}.{address.FunctionNumber:x}"),
            };
        }

        if (TryRead(adapter.Adapter, NativeMethods.KmtQueryGetSegmentSize, out NativeMethods.D3dkmtSegmentSizeInfo segments))
        {
            telemetry = telemetry with
            {
                DedicatedMemoryBytes = PositiveOrNull(segments.DedicatedVideoMemorySize),
                SharedMemoryBytes = PositiveOrNull(segments.SharedSystemMemorySize),
            };
        }

        var caps = default(NativeMethods.D3dkmtAdapterPerfDataCaps);
        var hasCaps = TryQuery(adapter.Adapter, NativeMethods.KmtQueryAdapterPerfDataCaps, ref caps);
        if (hasCaps)
        {
            telemetry = telemetry with
            {
                MaxTemperatureCelsius = caps.TemperatureMax > 0 ? caps.TemperatureMax / 10.0 : null,
                MaxFanRpm = caps.MaxFanRpm > 0 ? (int)caps.MaxFanRpm : null,
                MaxMemoryBandwidthBytesPerSecond = PositiveOrNull(caps.MaxMemoryBandwidth),
                MaxPcieBandwidthBytesPerSecond = PositiveOrNull(caps.MaxPcieBandwidth),
            };
        }

        var perf = default(NativeMethods.D3dkmtAdapterPerfData);
        if (TryQuery(adapter.Adapter, NativeMethods.KmtQueryAdapterPerfData, ref perf))
        {
            telemetry = telemetry with
            {
                TemperatureCelsius = perf.Temperature > 0 ? perf.Temperature / 10.0 : null,

                // 回転数 0 は「ファンが止まっている（低負荷で止める機種）」か「報告していない」のどちらか。
                // 最大回転数を報告している場合だけ、0 を「止まっている」として扱う。
                FanRpm = perf.FanRpm > 0 || telemetry.MaxFanRpm is not null ? (int)perf.FanRpm : null,
                PowerPercent = perf.Power > 0 ? perf.Power / 10.0 : null,
                MemoryClockMhz = HertzToMegahertz(perf.MemoryFrequency),
                MaxMemoryClockMhz = HertzToMegahertz(perf.MaxMemoryFrequency),
                // 帯域も同じく、最大値を報告している場合だけ 0 を「使っていない」として扱う。
                MemoryBandwidthBytesPerSecond = telemetry.MaxMemoryBandwidthBytesPerSecond is not null ? perf.MemoryBandwidth : PositiveOrNull(perf.MemoryBandwidth),
                PcieBandwidthBytesPerSecond = telemetry.MaxPcieBandwidthBytesPerSecond is not null ? perf.PcieBandwidth : PositiveOrNull(perf.PcieBandwidth),
            };
        }

        // ノード 0 は、ほとんどの GPU で 3D（描画）のエンジン。
        var node = default(NativeMethods.D3dkmtNodePerfData);
        if (TryQuery(adapter.Adapter, NativeMethods.KmtQueryNodePerfData, ref node))
        {
            telemetry = telemetry with
            {
                CoreClockMhz = HertzToMegahertz(node.Frequency),
                MaxCoreClockMhz = HertzToMegahertz(node.MaxFrequency),
                CoreVoltageMillivolts = node.Voltage > 0 ? (int)node.Voltage : null,
            };
        }

        return telemetry;
    }

    private static bool IsSoftwareAdapter(uint adapter) =>
        TryRead(adapter, NativeMethods.KmtQueryAdapterType, out uint flags)
        && (flags & NativeMethods.KmtAdapterTypeSoftwareDevice) != 0;

    private static unsafe string? ReadName(uint adapter)
    {
        // D3DKMT_ADAPTERREGISTRYINFO は WCHAR[260] が 4 つ並んだ構造体。最初の項目がアダプターの名前。
        const int length = NativeMethods.KmtRegistryStringLength * 4;
        var buffer = new char[length];
        fixed (char* pointer = buffer)
        {
            var query = new NativeMethods.D3dkmtQueryAdapterInfo
            {
                Adapter = adapter,
                Type = NativeMethods.KmtQueryAdapterRegistryInfo,
                PrivateDriverData = (nint)pointer,
                PrivateDriverDataSize = length * sizeof(char),
            };

            if (NativeMethods.D3DKMTQueryAdapterInfo(ref query) != 0)
            {
                return null;
            }
        }

        var name = new string(buffer, 0, NativeMethods.KmtRegistryStringLength);
        var end = name.IndexOf('\0');
        name = (end >= 0 ? name[..end] : name).Trim();
        return name.Length > 0 ? name : null;
    }

    private static bool TryRead<T>(uint adapter, int type, out T data)
        where T : unmanaged
    {
        data = default;
        return TryQuery(adapter, type, ref data);
    }

    private static unsafe bool TryQuery<T>(uint adapter, int type, ref T data)
        where T : unmanaged
    {
        fixed (T* pointer = &data)
        {
            var query = new NativeMethods.D3dkmtQueryAdapterInfo
            {
                Adapter = adapter,
                Type = type,
                PrivateDriverData = (nint)pointer,
                PrivateDriverDataSize = (uint)sizeof(T),
            };

            // 古いドライバーや Windows は、新しい問い合わせの種類に失敗を返す。その値は読めないものとして扱う。
            return NativeMethods.D3DKMTQueryAdapterInfo(ref query) == 0;
        }
    }

    private static ulong? PositiveOrNull(ulong value) => value > 0 ? value : null;

    private static double? HertzToMegahertz(ulong hertz) => hertz > 0 ? hertz / 1_000_000.0 : null;
}
