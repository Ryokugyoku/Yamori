using Yamori.Core.Interop;

namespace Yamori.Core.Hardware.Smbios;

/// <summary>
/// Windows の GetSystemFirmwareTable API で SMBIOS テーブルを読む。管理者権限もドライバーも不要。
/// </summary>
public sealed class FirmwareSmbiosTableSource : ISmbiosTableSource
{
    /// <summary>SMBIOS テーブルを表す署名 'RSMB'。</summary>
    private const uint RsmbSignature = 0x52534D42;

    private readonly SmbiosTableParser _parser;

    /// <summary>
    /// 解析処理を指定して作る。
    /// </summary>
    /// <param name="parser">バイト列の解析処理。省略時は新しく作る。</param>
    public FirmwareSmbiosTableSource(SmbiosTableParser? parser = null)
    {
        _parser = parser ?? new SmbiosTableParser();
    }

    /// <inheritdoc />
    public SmbiosTable? Read()
    {
        var size = NativeMethods.GetSystemFirmwareTable(RsmbSignature, 0, null, 0);
        if (size == 0)
        {
            return null;
        }

        var buffer = new byte[size];
        var written = NativeMethods.GetSystemFirmwareTable(RsmbSignature, 0, buffer, size);
        if (written == 0 || written > size)
        {
            return null;
        }

        return _parser.Parse(buffer.AsSpan(0, (int)written));
    }
}
