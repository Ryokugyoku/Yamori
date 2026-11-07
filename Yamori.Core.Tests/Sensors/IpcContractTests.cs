using System.Text;
using System.Text.Json;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Ipc;
using Yamori.Core.Sensors.Ipc.Messages;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Tests.Sensors;

public class IpcContractTests
{
    private static readonly SensorKey Package = SensorKey.Parse("cpu/0/temperature/package");
    private static readonly DateTimeOffset Time = new(2026, 11, 7, 1, 2, 3, TimeSpan.Zero);

    [Fact]
    public void DescriptorRoundTripsThroughJson()
    {
        var descriptor = new SensorDescriptor(Package, SensorUnit.Celsius, "CPU Package", "Intel Core i9-12900KS", "temperatures", "pawnio");

        var json = JsonSerializer.Serialize(new[] { SensorDtoMapper.ToDto(descriptor) }, SensorIpcJsonContext.Default.SensorDescriptorDtoArray);
        var back = SensorDtoMapper.FromDto(JsonSerializer.Deserialize(json, SensorIpcJsonContext.Default.SensorDescriptorDtoArray)![0]);

        Assert.Equal(descriptor, back);
        Assert.Contains("\"key\":\"cpu/0/temperature/package\"", json);
        Assert.Contains("\"unit\":2", json);
    }

    [Theory]
    [InlineData(SensorStatus.Ok, 54.5)]
    [InlineData(SensorStatus.Ok, 0.0)]
    [InlineData(SensorStatus.TemporarilyUnavailable, null)]
    [InlineData(SensorStatus.Error, null)]
    public void ReadingRoundTripsThroughJson(SensorStatus status, double? value)
    {
        var reading = new SensorReading(Package, value, Time, "pawnio", status);

        var json = JsonSerializer.Serialize(new[] { SensorDtoMapper.ToDto(reading) }, SensorIpcJsonContext.Default.SensorReadingDtoArray);
        var back = SensorDtoMapper.FromDto(JsonSerializer.Deserialize(json, SensorIpcJsonContext.Default.SensorReadingDtoArray)![0]);

        Assert.Equal(reading, back);
    }

    [Fact]
    public void HistoryPointAndPolicyRoundTripThroughJson()
    {
        var point = new SensorHistoryPoint(Time, 40, 60, 50.5, 6);
        var policy = new SensorLogPolicy(true, TimeSpan.FromSeconds(10), 0.5);

        var pointJson = JsonSerializer.Serialize(new[] { SensorDtoMapper.ToDto(point) }, SensorIpcJsonContext.Default.SensorHistoryPointDtoArray);
        var policyJson = JsonSerializer.Serialize(new SetLogPolicyRequest(Package.ToString(), SensorDtoMapper.ToDto(policy)), SensorIpcJsonContext.Default.SetLogPolicyRequest);

        Assert.Equal(point, SensorDtoMapper.FromDto(JsonSerializer.Deserialize(pointJson, SensorIpcJsonContext.Default.SensorHistoryPointDtoArray)![0]));
        Assert.Equal(policy, SensorDtoMapper.FromDto(JsonSerializer.Deserialize(policyJson, SensorIpcJsonContext.Default.SetLogPolicyRequest)!.Policy));
    }

    [Fact]
    public void InvalidKeysAndPoliciesAreRejected()
    {
        Assert.Null(SensorDtoMapper.FromDto(new SensorReadingDto("not/a/key", 1, Time, "x", SensorStatus.Ok)));
        Assert.Null(SensorDtoMapper.FromDto(new SensorDescriptorDto("cpu/0/unknown-type/x", SensorUnit.Celsius, "", "", "", "")));
        Assert.Null(SensorDtoMapper.FromDto(new SensorLogPolicyDto(true, 0, 0)));
        Assert.Null(SensorDtoMapper.FromDto(new SensorLogPolicyDto(true, 1000, -1)));
    }

    [Fact]
    public async Task EnvelopeRoundTripsThroughFraming()
    {
        var envelope = new IpcEnvelope
        {
            Kind = IpcMessageKind.Notification,
            Notification = SensorNotificationKind.ExtendedSensorStateChanged,
            Payload = IpcConnection.ToPayload(new ExtendedStateMessage(ExtendedSensorState.Active), SensorIpcJsonContext.Default.ExtendedStateMessage),
        };
        using var stream = new MemoryStream();
        await using (var writer = new IpcConnection(new NonClosingStream(stream)))
        {
            await writer.SendAsync(envelope, CancellationToken.None);
            await writer.SendAsync(envelope with { Kind = IpcMessageKind.Request, Id = 7, Operation = SensorOperation.GetCatalog, Payload = null }, CancellationToken.None);
        }

        stream.Position = 0;
        await using var reader = new IpcConnection(stream);
        var first = await reader.ReceiveAsync(CancellationToken.None);
        var second = await reader.ReceiveAsync(CancellationToken.None);
        var end = await reader.ReceiveAsync(CancellationToken.None);

        Assert.Equal(IpcMessageKind.Notification, first!.Kind);
        Assert.Equal(ExtendedSensorState.Active, IpcConnection.FromPayload(first.Payload, SensorIpcJsonContext.Default.ExtendedStateMessage)!.State);
        Assert.Equal(7, second!.Id);
        Assert.Equal(SensorOperation.GetCatalog, second.Operation);
        Assert.Null(end);
    }

    [Fact]
    public void UnknownFieldsAndEnumNumbersFromNewerPeersAreAccepted()
    {
        var json = """{"kind":2,"id":3,"operation":99,"futureField":{"a":1}}""";

        var envelope = JsonSerializer.Deserialize(json, SensorIpcJsonContext.Default.IpcEnvelope)!;

        Assert.Equal(IpcMessageKind.Request, envelope.Kind);
        Assert.Equal((SensorOperation)99, envelope.Operation);
    }

    [Fact]
    public async Task OversizedOrTruncatedFramesAreRejected()
    {
        using var oversized = new MemoryStream(BitConverter.GetBytes(IpcFraming.MaxFrameBytes + 1));
        using var truncated = new MemoryStream([5, 0, 0, 0, (byte)'{']);

        await Assert.ThrowsAsync<InvalidDataException>(() => IpcFraming.ReadFrameAsync(oversized, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidDataException>(() => IpcFraming.ReadFrameAsync(truncated, CancellationToken.None));
    }

    [Fact]
    public async Task BrokenJsonIsRejected()
    {
        using var stream = new MemoryStream();
        await IpcFraming.WriteFrameAsync(stream, Encoding.UTF8.GetBytes("{oops"), CancellationToken.None);
        stream.Position = 0;
        await using var connection = new IpcConnection(stream);

        await Assert.ThrowsAsync<InvalidDataException>(() => connection.ReceiveAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData(SensorContract.Version, true)]
    [InlineData(SensorContract.Version + 5, true)]
    [InlineData(SensorContract.MinimumCompatibleVersion - 1, false)]
    public void CompatibilityFollowsMinimumVersion(int peer, bool expected) =>
        Assert.Equal(expected, SensorContract.IsCompatible(peer));

    /// <summary>書き込み側を閉じても、下のストリームを閉じないようにする。</summary>
    private sealed class NonClosingStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => inner.CanSeek;

        public override bool CanWrite => inner.CanWrite;

        public override long Length => inner.Length;

        public override long Position { get => inner.Position; set => inner.Position = value; }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value) => inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
        }
    }
}
