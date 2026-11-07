using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Sensors.Service;

/// <summary>
/// GUI から見たセンサーの窓口。GUI はこれだけを使い、ストアのファイルを直接開かない。
/// 将来は Helper（サービス）への IPC で実装し、今は同じ形のまま GUI のプロセスの中で動かす（<see cref="LocalSensorService"/>）。
/// </summary>
public interface ISensorService
{
    /// <summary>やり取りの版（<see cref="SensorContract.Version"/>）を取得する。</summary>
    int ContractVersion { get; }

    /// <summary>
    /// 新しい値を読んだときに発生する。引数は読んだすべての値。UI スレッド以外から発生する。
    /// </summary>
    event EventHandler<IReadOnlyList<SensorReading>>? ReadingsUpdated;

    /// <summary>
    /// 利用可能なセンサーの一覧が変わったときに発生する。引数は新しい一覧。UI スレッド以外から発生する。
    /// </summary>
    event EventHandler<IReadOnlyList<SensorDescriptor>>? CatalogChanged;

    /// <summary>
    /// 利用可能なセンサーの一覧を取得する。
    /// </summary>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>キーの文字列の順に並べた一覧。</returns>
    Task<IReadOnlyList<SensorDescriptor>> GetCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定したセンサーの最新値を取得する。
    /// </summary>
    /// <param name="keys">センサーのキー。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>キーごとの最新値。カタログにないキーは <see cref="SensorStatus.Unsupported"/>、まだ読んでいないキーは <see cref="SensorStatus.TemporarilyUnavailable"/>。</returns>
    Task<IReadOnlyList<SensorReading>> GetLatestAsync(IReadOnlyCollection<SensorKey> keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// 直近の区間の履歴を、古い順に取得する。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <param name="window">今からさかのぼる長さ。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>区間内の値。なければ空。</returns>
    Task<IReadOnlyList<SensorReading>> GetRecentAsync(SensorKey key, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>
    /// 拡張センサー（PawnIO）が使えるかどうかを取得する。UI はこれを 1 か所にまとめて表示する。
    /// </summary>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>拡張センサーの状態。</returns>
    Task<ExtendedSensorState> GetExtendedSensorStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 時系列のログから履歴を古い順に読む。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <param name="fromUtc">範囲の始まり（含む）。</param>
    /// <param name="toUtc">範囲の終わり（含まない）。</param>
    /// <param name="resolution">解像度。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>範囲内の点。記録していない、または記録が使えない場合は空。</returns>
    Task<IReadOnlyList<SensorHistoryPoint>> GetHistoryAsync(
        SensorKey key,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        TimeSeriesResolution resolution,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// センサーの記録の設定を取得する。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>記録の設定。マスタにないか、記録が使えない場合は null。</returns>
    Task<SensorLogPolicy?> GetLogPolicyAsync(SensorKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// センサーの記録の設定を変える。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <param name="policy">新しい設定。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>変えられたら true。マスタにないか、記録が使えない場合は false。</returns>
    Task<bool> SetLogPolicyAsync(SensorKey key, SensorLogPolicy policy, CancellationToken cancellationToken = default);
}
