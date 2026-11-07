using System.Buffers.Binary;

namespace Yamori.Core.Hardware.DeviceIo;

/// <summary>
/// IOCTL_STORAGE_QUERY_PROPERTY（StorageDeviceTemperatureProperty）が返す STORAGE_TEMPERATURE_DATA_DESCRIPTOR を読み解く。
/// </summary>
/// <remarks>
/// 先頭 24 バイトが見出し（Version、Size、CriticalTemperature、WarningTemperature、InfoCount など）で、
/// その後に 16 バイトの STORAGE_TEMPERATURE_INFO（Index、Temperature など）が InfoCount 個並ぶ。
/// </remarks>
public static class StorageTemperatureDescriptorParser
{
    /// <summary>見出しの大きさ（バイト）。</summary>
    public const int HeaderSize = 24;

    /// <summary>STORAGE_TEMPERATURE_INFO 1 件の大きさ（バイト）。</summary>
    public const int InfoSize = 16;

    private const int InfoCountOffset = 12;
    private const int InfoIndexOffset = 0;
    private const int InfoTemperatureOffset = 2;

    /// <summary>ディスク全体を代表する温度（NVMe の Composite Temperature など）の番号。</summary>
    private const ushort CompositeIndex = 0;

    /// <summary>温度として扱う範囲の下限（℃）。これより低い値は「読めない」を表す値とみなす。</summary>
    private const int MinimumCelsius = -40;

    /// <summary>温度として扱う範囲の上限（℃）。</summary>
    private const int MaximumCelsius = 150;

    /// <summary>
    /// ディスク全体を代表する温度を取り出す。番号 0 の値を優先し、ない場合は最初の値にする。
    /// </summary>
    /// <param name="descriptor">ドライバーが返したバイト列（返された長さに切り詰めたもの）。</param>
    /// <returns>温度（℃）。形が違うか、値が範囲外の場合は null。</returns>
    public static int? ParseCelsius(ReadOnlySpan<byte> descriptor)
    {
        if (descriptor.Length < HeaderSize + InfoSize)
        {
            return null;
        }

        var count = BinaryPrimitives.ReadUInt16LittleEndian(descriptor[InfoCountOffset..]);
        int? first = null;
        for (var i = 0; i < count; i++)
        {
            var offset = HeaderSize + (i * InfoSize);
            if (offset + InfoSize > descriptor.Length)
            {
                break;
            }

            var info = descriptor.Slice(offset, InfoSize);
            var temperature = BinaryPrimitives.ReadInt16LittleEndian(info[InfoTemperatureOffset..]);
            if (temperature is < MinimumCelsius or > MaximumCelsius)
            {
                continue;
            }

            if (BinaryPrimitives.ReadUInt16LittleEndian(info[InfoIndexOffset..]) == CompositeIndex)
            {
                return temperature;
            }

            first ??= temperature;
        }

        return first;
    }
}
