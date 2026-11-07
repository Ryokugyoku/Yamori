using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Providers;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// 宣言するセンサーと、読んだときの値をテストから決められる Provider。
/// </summary>
internal sealed class FakeSensorProvider(string providerId) : ISensorProvider
{
    private static readonly DateTimeOffset Timestamp = new(2026, 11, 7, 0, 0, 0, TimeSpan.Zero);

    public string ProviderId { get; } = providerId;

    public List<SensorDescriptor> Descriptors { get; } = [];

    /// <summary>キーごとの値。ない場合は TemporarilyUnavailable を返す。</summary>
    public Dictionary<SensorKey, double> Values { get; } = [];

    /// <summary>キーごとの、値の代わりに返す状態。</summary>
    public Dictionary<SensorKey, SensorStatus> Statuses { get; } = [];

    public bool ThrowOnDiscover { get; set; }

    public bool ThrowOnRead { get; set; }

    public List<IReadOnlyCollection<SensorKey>> ReadRequests { get; } = [];

    public FakeSensorProvider Declare(SensorKey key, double? value = null, string displayName = "sensor")
    {
        Descriptors.Add(new SensorDescriptor(key, SensorUnits.ForType(key.SensorType), displayName, "hw", "group", ProviderId));
        if (value is { } v)
        {
            Values[key] = v;
        }

        return this;
    }

    public Task<IReadOnlyList<SensorDescriptor>> DiscoverAsync(CancellationToken cancellationToken) =>
        ThrowOnDiscover
            ? throw new InvalidOperationException("discover failed")
            : Task.FromResult<IReadOnlyList<SensorDescriptor>>(Descriptors.ToArray());

    public Task<IReadOnlyList<SensorReading>> ReadAsync(IReadOnlyCollection<SensorKey> keys, CancellationToken cancellationToken)
    {
        ReadRequests.Add(keys.ToArray());
        if (ThrowOnRead)
        {
            throw new InvalidOperationException("read failed");
        }

        IReadOnlyList<SensorReading> result = keys
            .Select(k => Statuses.TryGetValue(k, out var status)
                ? SensorReading.Failed(k, status, Timestamp, ProviderId)
                : Values.TryGetValue(k, out var value)
                    ? SensorReading.Ok(k, value, Timestamp, ProviderId)
                    : SensorReading.Failed(k, SensorStatus.TemporarilyUnavailable, Timestamp, ProviderId))
            .ToArray();
        return Task.FromResult(result);
    }
}
