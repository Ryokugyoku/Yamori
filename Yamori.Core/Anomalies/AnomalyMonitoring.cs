using Yamori.Core.Anomalies.Detection;
using Yamori.Core.Anomalies.Service;
using Yamori.Core.Anomalies.Storage;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Master;

namespace Yamori.Core.Anomalies;

/// <summary>
/// Helper の中で、異常の見張り（検出の仕組み・一覧と設定の保存・窓口）をまとめて持つ。
/// </summary>
public sealed class AnomalyMonitoring
{
    private readonly AnomalyDetector _detector;

    private AnomalyMonitoring(AnomalyDetector detector, LocalAnomalyService service)
    {
        _detector = detector;
        Service = service;
    }

    /// <summary>異常の一覧と設定の窓口を取得する。</summary>
    public IAnomalyService Service { get; }

    /// <summary>
    /// マスタに異常の一覧と設定を置く構成で作る。
    /// </summary>
    /// <param name="master">マスタのデータベース（スキーマの版 2 以上）。</param>
    /// <param name="rules">判定の規則。</param>
    /// <param name="timeProvider">現在時刻の提供元。</param>
    /// <returns>新しいインスタンス。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static AnomalyMonitoring Create(MasterDatabase master, IEnumerable<IAnomalyRule> rules, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(master);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var repository = new SqliteAnomalyRepository(master);
        var settings = new AnomalySettingsCache(new SqliteAnomalySettingsRepository(master));
        var detector = new AnomalyDetector(rules, repository, settings, new AnomalyNotificationPolicy(), timeProvider);
        return new AnomalyMonitoring(detector, new LocalAnomalyService(detector, repository, settings, timeProvider));
    }

    /// <summary>
    /// 見張りを始める前に呼ぶ（前回終わらせられなかった異常を終わらせる）。
    /// </summary>
    public void Start() => _detector.Start();

    /// <summary>
    /// カタログが変わったときに呼ぶ。
    /// </summary>
    /// <param name="catalog">新しいカタログ。</param>
    public void OnCatalogChanged(IReadOnlyList<SensorDescriptor> catalog) => _detector.OnCatalogChanged(catalog);

    /// <summary>
    /// 新しく読んだ値で判定する。
    /// </summary>
    /// <param name="readings">読んだ値。</param>
    public void OnReadings(IReadOnlyList<SensorReading> readings) => _detector.OnReadings(readings);
}
