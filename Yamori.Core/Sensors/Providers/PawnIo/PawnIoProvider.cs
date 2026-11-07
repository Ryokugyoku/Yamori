namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// PawnIO を使って、CPU の温度・電力・実クロック、電圧、ファン、メモリの温度などを読む Provider（今はスタブ）。
/// Helper のプロセスでだけ登録し、ハードウェアには読み取りしか行わない。
/// </summary>
/// <remarks>
/// スタブのため、宣言は常に空で、読み取りは常に <see cref="SensorStatus.Unsupported"/> を返す。
/// 実装するときは、標準 API と意味が完全に同じ値だけ同じキーを使い（例：<c>cpu/0/temperature/package</c>）、
/// MSR から読む実クロックは推定値（<c>…-estimated</c>）とは別のキーにすること。
/// </remarks>
public sealed class PawnIoProvider : ISensorProvider
{
    /// <inheritdoc />
    public string ProviderId => SensorProviderIds.PawnIo;

    /// <inheritdoc />
    public Task<IReadOnlyList<SensorDescriptor>> DiscoverAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SensorDescriptor>>([]);

    /// <inheritdoc />
    public Task<IReadOnlyList<SensorReading>> ReadAsync(IReadOnlyCollection<SensorKey> keys, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var now = DateTimeOffset.UtcNow;
        IReadOnlyList<SensorReading> result = keys
            .Select(k => SensorReading.Failed(k, SensorStatus.Unsupported, now, ProviderId))
            .ToArray();
        return Task.FromResult(result);
    }
}
