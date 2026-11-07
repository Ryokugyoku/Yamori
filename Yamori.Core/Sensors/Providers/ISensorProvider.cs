namespace Yamori.Core.Sensors.Providers;

/// <summary>
/// センサーの値の取得元（Windows の標準 API、PawnIO など）。
/// </summary>
/// <remarks>
/// 提供できるかどうかは <see cref="DiscoverAsync"/> の宣言で決める。<see cref="ReadAsync"/> で読めなかった値は
/// <see cref="SensorStatus.TemporarilyUnavailable"/> などの状態で返し、null から「提供していない」とは推測しない。
/// 使えない取得元（PawnIO が入っていないなど）は、登録しないだけにする。
/// </remarks>
public interface ISensorProvider
{
    /// <summary>取得元の ID（例：<c>win-std</c>、<c>pawnio</c>）を取得する。<see cref="SensorProviderIds"/> を参照。</summary>
    string ProviderId { get; }

    /// <summary>
    /// この環境で提供できるセンサーを調べて宣言する。起動したときと、ハードウェアの構成が変わったときに呼ぶ。
    /// </summary>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>提供できるセンサーの一覧。何も提供できなければ空。</returns>
    Task<IReadOnlyList<SensorDescriptor>> DiscoverAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 指定したセンサーの今の値を読む。
    /// </summary>
    /// <param name="keys">読むセンサーのキー。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>キーごとの結果。最後の <see cref="DiscoverAsync"/> で宣言していないキーは <see cref="SensorStatus.Unsupported"/>。</returns>
    Task<IReadOnlyList<SensorReading>> ReadAsync(IReadOnlyCollection<SensorKey> keys, CancellationToken cancellationToken);
}
