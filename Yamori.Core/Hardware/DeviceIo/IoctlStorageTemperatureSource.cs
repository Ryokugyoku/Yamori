using System.Buffers.Binary;
using System.Globalization;
using Yamori.Core.Interop;

namespace Yamori.Core.Hardware.DeviceIo;

/// <summary>
/// <c>\\.\PhysicalDriveN</c> をアクセス権 0 で開き、IOCTL_STORAGE_QUERY_PROPERTY（StorageDeviceTemperatureProperty）で温度を読む。
/// </summary>
/// <remarks>
/// アクセス権 0 のハンドルはディスクの中身を読み書きできず、属性の問い合わせだけができる。そのため管理者権限は不要。
/// NVMe と SATA のディスクの多くは Windows 10 以降の標準ドライバーで対応している。対応していないディスク（仮想ディスクや一部の USB 接続）は null を返す。
/// </remarks>
public sealed class IoctlStorageTemperatureSource : IStorageTemperatureSource
{
    /// <summary>出力のバッファーの大きさ（バイト）。温度センサー 8 個分あれば足りる。</summary>
    private const int OutputBufferSize = StorageTemperatureDescriptorParser.HeaderSize + (StorageTemperatureDescriptorParser.InfoSize * 8);

    /// <inheritdoc />
    public int? ReadCelsius(int diskNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(diskNumber);

        var path = string.Create(CultureInfo.InvariantCulture, $@"\\.\PhysicalDrive{diskNumber}");
        using var device = NativeMethods.CreateFile(
            path,
            desiredAccess: 0,
            NativeMethods.FileShareRead | NativeMethods.FileShareWrite,
            securityAttributes: 0,
            NativeMethods.OpenExisting,
            flagsAndAttributes: 0,
            templateFile: 0);
        if (device.IsInvalid)
        {
            return null;
        }

        var query = new byte[NativeMethods.StoragePropertyQuerySize];
        BinaryPrimitives.WriteInt32LittleEndian(query, NativeMethods.StorageDeviceTemperatureProperty);
        BinaryPrimitives.WriteInt32LittleEndian(query.AsSpan(sizeof(int)), NativeMethods.PropertyStandardQuery);

        var output = new byte[OutputBufferSize];
        if (!NativeMethods.DeviceIoControl(
                device,
                NativeMethods.IoctlStorageQueryProperty,
                query,
                query.Length,
                output,
                output.Length,
                out var returned,
                overlapped: 0))
        {
            // 対応していないディスク（ERROR_INVALID_FUNCTION など）。値なしとして扱う。
            return null;
        }

        return StorageTemperatureDescriptorParser.ParseCelsius(output.AsSpan(0, Math.Clamp(returned, 0, output.Length)));
    }
}
