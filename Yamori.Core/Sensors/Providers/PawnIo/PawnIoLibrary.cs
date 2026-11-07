using System.Runtime.InteropServices;
using System.Text;

namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// PawnIO のライブラリ（<c>PawnIOLib.dll</c>、PawnIO と一緒にユーザーが入れるもの）の関数を呼ぶ。開く・モジュールを読み込む・関数を実行する・閉じるだけを使う。
/// </summary>
/// <remarks>関数の形は PawnIO に付いている <c>PawnIOLib.h</c> に従う。戻り値は HRESULT。</remarks>
public sealed unsafe class PawnIoLibrary : IDisposable
{
    private const string LibraryFileName = "PawnIOLib.dll";

    private readonly nint _library;
    private readonly delegate* unmanaged[Stdcall]<nint*, int> _open;
    private readonly delegate* unmanaged[Stdcall]<nint, byte*, nuint, int> _load;
    private readonly delegate* unmanaged[Stdcall]<nint, byte*, ulong*, nuint, ulong*, nuint, nuint*, int> _execute;
    private readonly delegate* unmanaged[Stdcall]<nint, int> _close;
    private bool _disposed;

    private PawnIoLibrary(nint library)
    {
        _library = library;
        _open = (delegate* unmanaged[Stdcall]<nint*, int>)NativeLibrary.GetExport(library, "pawnio_open");
        _load = (delegate* unmanaged[Stdcall]<nint, byte*, nuint, int>)NativeLibrary.GetExport(library, "pawnio_load");
        _execute = (delegate* unmanaged[Stdcall]<nint, byte*, ulong*, nuint, ulong*, nuint, nuint*, int>)NativeLibrary.GetExport(library, "pawnio_execute");
        _close = (delegate* unmanaged[Stdcall]<nint, int>)NativeLibrary.GetExport(library, "pawnio_close");
    }

    /// <summary>
    /// PawnIO を入れたフォルダーからライブラリを読み込む。
    /// </summary>
    /// <param name="installDirectory">PawnIO を入れたフォルダー（<see cref="PawnIoInstallLocator.TryFindInstallDirectory"/>）。</param>
    /// <returns>読み込んだライブラリ。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="installDirectory"/> が null。</exception>
    /// <exception cref="DllNotFoundException">ライブラリが見つからない、または必要な関数がない。</exception>
    public static PawnIoLibrary Load(string installDirectory)
    {
        ArgumentNullException.ThrowIfNull(installDirectory);

        var path = Path.Combine(installDirectory, LibraryFileName);
        if (!NativeLibrary.TryLoad(path, out var library))
        {
            throw new DllNotFoundException($"PawnIO のライブラリを読み込めません: {path}");
        }

        try
        {
            return new PawnIoLibrary(library);
        }
        catch (EntryPointNotFoundException ex)
        {
            NativeLibrary.Free(library);
            throw new DllNotFoundException("PawnIO のライブラリに必要な関数がありません。", ex);
        }
    }

    /// <summary>
    /// PawnIO の実行器を開く。
    /// </summary>
    /// <returns>実行器のハンドル。</returns>
    /// <exception cref="COMException">開けなかった（管理者でない、ドライバーが動いていないなど）。</exception>
    public nint Open()
    {
        nint handle;
        ThrowIfFailed(_open(&handle), "pawnio_open");
        return handle;
    }

    /// <summary>
    /// モジュール（署名済みのブロブ）を読み込む。
    /// </summary>
    /// <param name="handle">実行器のハンドル。</param>
    /// <param name="module">モジュールの中身。</param>
    /// <exception cref="COMException">読み込めなかった（署名が正しくない、CPU が対応していないなど）。</exception>
    public void LoadModule(nint handle, ReadOnlySpan<byte> module)
    {
        fixed (byte* blob = module)
        {
            ThrowIfFailed(_load(handle, blob, (nuint)module.Length), "pawnio_load");
        }
    }

    /// <summary>
    /// モジュールの関数を実行する。
    /// </summary>
    /// <param name="handle">実行器のハンドル。</param>
    /// <param name="function">関数の名前（例：<c>ioctl_read_msr</c>）。</param>
    /// <param name="input">入力。</param>
    /// <param name="output">出力を受け取る。</param>
    /// <returns>成功したら true（HRESULT が 0 以上）。</returns>
    public bool TryExecute(nint handle, string function, ReadOnlySpan<ulong> input, Span<ulong> output)
    {
        var name = Encoding.ASCII.GetBytes(function + "\0");
        fixed (byte* namePtr = name)
        fixed (ulong* inPtr = input)
        fixed (ulong* outPtr = output)
        {
            nuint returned = 0;
            var result = _execute(handle, namePtr, inPtr, (nuint)input.Length, outPtr, (nuint)output.Length, &returned);
            return result >= 0 && returned >= (nuint)output.Length;
        }
    }

    /// <summary>
    /// 実行器を閉じる。
    /// </summary>
    /// <param name="handle">実行器のハンドル。</param>
    public void Close(nint handle)
    {
        if (handle != 0)
        {
            _close(handle);
        }
    }

    /// <summary>
    /// ライブラリを解放する。開いた実行器は先に <see cref="Close"/> で閉じること。
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            NativeLibrary.Free(_library);
        }
    }

    private static void ThrowIfFailed(int hresult, string function)
    {
        if (hresult < 0)
        {
            throw new COMException($"{function} が失敗しました（0x{hresult:X8}）。", hresult);
        }
    }
}
