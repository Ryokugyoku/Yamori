using System.Buffers.Binary;
using Yamori.Core.Hardware.DeviceIo;

namespace Yamori.Core.Tests.Hardware;

public class StorageTemperatureDescriptorParserTests
{
    private static byte[] Descriptor(params (ushort Index, short Celsius)[] sensors)
    {
        var bytes = new byte[StorageTemperatureDescriptorParser.HeaderSize + (sensors.Length * StorageTemperatureDescriptorParser.InfoSize)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, 1);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), bytes.Length);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(8), 88);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(10), 84);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12), (ushort)sensors.Length);
        for (var i = 0; i < sensors.Length; i++)
        {
            var offset = StorageTemperatureDescriptorParser.HeaderSize + (i * StorageTemperatureDescriptorParser.InfoSize);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), sensors[i].Index);
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(offset + 2), sensors[i].Celsius);
        }

        return bytes;
    }

    [Fact]
    public void PrefersCompositeSensor()
    {
        Assert.Equal(43, StorageTemperatureDescriptorParser.ParseCelsius(Descriptor((1, 50), (0, 43))));
    }

    [Fact]
    public void FallsBackToFirstValidSensor()
    {
        Assert.Equal(50, StorageTemperatureDescriptorParser.ParseCelsius(Descriptor((1, 50), (2, 55))));
    }

    [Fact]
    public void SkipsOutOfRangeValues()
    {
        Assert.Equal(31, StorageTemperatureDescriptorParser.ParseCelsius(Descriptor((0, short.MinValue), (1, 31))));
    }

    [Fact]
    public void ReturnsNullForShortOrEmptyDescriptors()
    {
        Assert.Null(StorageTemperatureDescriptorParser.ParseCelsius(new byte[16]));
        Assert.Null(StorageTemperatureDescriptorParser.ParseCelsius(Descriptor()));
    }

    [Fact]
    public void StopsAtTruncatedData()
    {
        var bytes = Descriptor((1, 40), (0, 45));
        Assert.Equal(40, StorageTemperatureDescriptorParser.ParseCelsius(bytes.AsSpan(0, bytes.Length - 1)));
    }
}
