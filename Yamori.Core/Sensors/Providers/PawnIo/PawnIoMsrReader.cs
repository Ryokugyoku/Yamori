namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// PawnIO と、PawnIO.Modules の IntelMSR モジュールで MSR を読む <see cref="IMsrReader"/>。
/// 許可リスト（<see cref="IntelMsrRegisters.AllowedReads"/>）にない MSR は PawnIO に渡さない。書き込みの関数は呼ばない。
/// </summary>
public sealed class PawnIoMsrReader : IMsrReader
{
    /// <summary>IntelMSR モジュールの、MSR を 1 つ読む関数の名前（入力：MSR の番号、出力：値）。</summary>
    public const string ReadMsrFunction = "ioctl_read_msr";

    private readonly PawnIoLibrary _library;
    private readonly nint _handle;
    private readonly Lock _gate = new();
    private bool _disposed;

    private PawnIoMsrReader(PawnIoLibrary library, nint handle)
    {
        _library = library;
        _handle = handle;
    }

    /// <summary>
    /// PawnIO を開き、IntelMSR モジュールを読み込む。
    /// </summary>
    /// <param name="installDirectory">PawnIO を入れたフォルダー。</param>
    /// <param name="modulePath">IntelMSR モジュール（<c>IntelMSR.bin</c>）のパス。</param>
    /// <returns>開いた読み取り器。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    /// <exception cref="DllNotFoundException">PawnIO のライブラリがない。</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">開けない、またはモジュールを読み込めない。</exception>
    /// <exception cref="IOException">モジュールのファイルを読めない。</exception>
    public static PawnIoMsrReader Open(string installDirectory, string modulePath)
    {
        ArgumentNullException.ThrowIfNull(installDirectory);
        ArgumentNullException.ThrowIfNull(modulePath);

        var module = File.ReadAllBytes(modulePath);
        var library = PawnIoLibrary.Load(installDirectory);
        nint handle = 0;
        try
        {
            handle = library.Open();
            library.LoadModule(handle, module);
            return new PawnIoMsrReader(library, handle);
        }
        catch
        {
            library.Close(handle);
            library.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public bool TryRead(CpuCore? core, ReadOnlySpan<uint> registers, Span<ulong> values)
    {
        if (values.Length != registers.Length)
        {
            throw new ArgumentException("値の数が MSR の数と違います。", nameof(values));
        }

        foreach (var register in registers)
        {
            if (!IntelMsrRegisters.AllowedReads.Contains(register))
            {
                return false;
            }
        }

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            using var pin = core is null ? null : ThreadAffinityScope.TryPin(core.Group, core.LogicalProcessor);
            if (core is not null && pin is null)
            {
                return false;
            }

            Span<ulong> input = stackalloc ulong[1];
            Span<ulong> output = stackalloc ulong[1];
            for (var i = 0; i < registers.Length; i++)
            {
                input[0] = registers[i];
                if (!_library.TryExecute(_handle, ReadMsrFunction, input, output))
                {
                    return false;
                }

                values[i] = output[0];
            }

            return true;
        }
    }

    /// <summary>
    /// PawnIO を閉じる。
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _library.Close(_handle);
            _library.Dispose();
        }
    }
}
