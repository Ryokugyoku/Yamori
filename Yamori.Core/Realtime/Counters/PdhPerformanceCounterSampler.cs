using System.Globalization;
using System.Runtime.InteropServices;
using Yamori.Core.Interop;

namespace Yamori.Core.Realtime.Counters;

/// <summary>
/// PDH（Performance Data Helper）でパフォーマンスカウンターを読む。カウンターは英語名で指定するため、Windows の表示言語に左右されない。
/// </summary>
/// <remarks>
/// この PC のカウンターだけを読み、ネットワーク越しの接続はしない。スレッドセーフではない（1 つのスレッドから使うこと）。
/// </remarks>
public sealed class PdhPerformanceCounterSampler : IPerformanceCounterSampler
{
    private static readonly IReadOnlyDictionary<string, double> Empty = new Dictionary<string, double>();

    private readonly Dictionary<string, nint> _counters = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _unavailable = new(StringComparer.OrdinalIgnoreCase);
    private nint _query;
    private nint _buffer;
    private uint _bufferSize;

    /// <summary>
    /// クエリを開いて作る。
    /// </summary>
    /// <exception cref="InvalidOperationException">PDH のクエリを開けなかった。</exception>
    public PdhPerformanceCounterSampler()
    {
        var status = NativeMethods.PdhOpenQuery(null, 0, out _query);
        if (status != NativeMethods.PdhSuccess)
        {
            throw new InvalidOperationException($"パフォーマンスカウンターのクエリを開けませんでした（0x{status:X8}）。");
        }
    }

    /// <inheritdoc />
    public void Collect()
    {
        ObjectDisposedException.ThrowIf(_query == 0, this);

        // 失敗（カウンターが一時的に消えたなど）は、値が読めないこととして Read 側で扱う。
        _ = NativeMethods.PdhCollectQueryData(_query);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, double> Read(string counterPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(counterPath);
        ObjectDisposedException.ThrowIf(_query == 0, this);

        if (!_counters.TryGetValue(counterPath, out var counter))
        {
            if (!_unavailable.Contains(counterPath))
            {
                TryAddCounter(counterPath);
            }

            // 加えたばかりのカウンターは、次の Collect の後に読めるようになる。
            return Empty;
        }

        return ReadFormatted(counter);
    }

    /// <inheritdoc />
    public bool IsAvailable(string counterPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(counterPath);
        ObjectDisposedException.ThrowIf(_query == 0, this);

        if (!_counters.ContainsKey(counterPath) && !_unavailable.Contains(counterPath))
        {
            TryAddCounter(counterPath);
        }

        return _counters.ContainsKey(counterPath);
    }

    /// <summary>
    /// クエリと作業用のバッファーを解放する。
    /// </summary>
    public void Dispose()
    {
        if (_query != 0)
        {
            _ = NativeMethods.PdhCloseQuery(_query);
            _query = 0;
        }

        if (_buffer != 0)
        {
            Marshal.FreeHGlobal(_buffer);
            _buffer = 0;
            _bufferSize = 0;
        }

        _counters.Clear();
    }

    private void TryAddCounter(string counterPath)
    {
        var status = NativeMethods.PdhAddEnglishCounter(_query, counterPath, 0, out var counter);
        if (status != NativeMethods.PdhSuccess)
        {
            // この PC にないカウンター（古い Windows の GPU カウンターなど）は、毎回試さないよう覚えておく。
            _unavailable.Add(counterPath);
            return;
        }

        _counters[counterPath] = counter;
    }

    private IReadOnlyDictionary<string, double> ReadFormatted(nint counter)
    {
        const uint format = NativeMethods.PdhFormatDouble | NativeMethods.PdhFormatNoCap100;

        var size = _bufferSize;
        var status = NativeMethods.PdhGetFormattedCounterArray(counter, format, ref size, out var count, _buffer);
        if (status == NativeMethods.PdhMoreData)
        {
            EnsureBuffer(size);
            size = _bufferSize;
            status = NativeMethods.PdhGetFormattedCounterArray(counter, format, ref size, out count, _buffer);
        }

        if (status != NativeMethods.PdhSuccess || count == 0)
        {
            return Empty;
        }

        var values = new Dictionary<string, double>((int)count, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < count; i++)
        {
            var item = _buffer + (i * NativeMethods.PdhFormattedItemSize);
            var itemStatus = (uint)Marshal.ReadInt32(item, NativeMethods.PdhFormattedItemStatusOffset);
            if (itemStatus > 1)
            {
                // PDH_CSTATUS_VALID_DATA (0) と PDH_CSTATUS_NEW_DATA (1) 以外は、まだ計算できていない値。
                continue;
            }

            var name = Marshal.PtrToStringUni(Marshal.ReadIntPtr(item)) ?? string.Empty;
            var value = BitConverter.Int64BitsToDouble(Marshal.ReadInt64(item, NativeMethods.PdhFormattedItemValueOffset));

            // 同じ名前のインスタンス（\Process の同名のプロセスなど）は同じ名前のまま返るため、
            // パフォーマンス モニターと同じく 2 つ目から "#1"、"#2" を付けて見分ける。どのカウンターでも同じ順で返る。
            values[DisambiguateName(values, name)] = value;
        }

        return values;
    }

    private static string DisambiguateName(Dictionary<string, double> values, string name)
    {
        if (!values.ContainsKey(name))
        {
            return name;
        }

        var index = 1;
        string candidate;
        do
        {
            candidate = string.Create(CultureInfo.InvariantCulture, $"{name}#{index++}");
        }
        while (values.ContainsKey(candidate));

        return candidate;
    }

    private void EnsureBuffer(uint size)
    {
        if (size <= _bufferSize)
        {
            return;
        }

        if (_buffer != 0)
        {
            Marshal.FreeHGlobal(_buffer);
        }

        // インスタンスが増えたときに何度も確保し直さないよう、少し余裕を持たせる。
        var allocated = size + (size / 4);
        _buffer = Marshal.AllocHGlobal((nint)allocated);
        _bufferSize = allocated;
    }
}
