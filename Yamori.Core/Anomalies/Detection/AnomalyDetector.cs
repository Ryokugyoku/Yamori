using Yamori.Core.Anomalies.Storage;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Master;

namespace Yamori.Core.Anomalies.Detection;

/// <summary>
/// Helper が毎秒読んだ値を、保存を待たずにメモリ上で規則（<see cref="IAnomalyRule"/>）に渡し、異常の始まり・重くなった・終わりを
/// 異常の一覧（<see cref="IAnomalyRepository"/>）に残して知らせる。規則は足していける。
/// </summary>
/// <remarks>
/// <para>同じ部品の同じ種類の異常が続いている間は、同じ 1 行を更新する。続いている間の書き込みは、重くなったときと
/// <see cref="DefaultPersistInterval"/> ごとだけにする（毎秒は書かない）。</para>
/// <para>規則やマスタの失敗では例外を投げない（収集を止めないため）。その回の判定を飛ばし、次の回にやり直す。スレッドセーフ。</para>
/// </remarks>
public sealed class AnomalyDetector
{
    /// <summary>続いている異常の「最後に確かめた時刻」をマスタに書く既定の間隔。</summary>
    public static readonly TimeSpan DefaultPersistInterval = TimeSpan.FromSeconds(30);

    private readonly IReadOnlyList<IAnomalyRule> _rules;
    private readonly IAnomalyRepository _repository;
    private readonly AnomalySettingsCache _settings;
    private readonly AnomalyNotificationPolicy _notifications;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _persistInterval;
    private readonly Lock _gate = new();
    private readonly Dictionary<SensorKey, SensorReading> _latest = [];
    private readonly Dictionary<(AnomalyKind Kind, string HardwareKey), OpenAnomaly> _open = [];
    private Dictionary<string, string> _hardwareNames = new(StringComparer.Ordinal);

    /// <summary>
    /// 部品を指定して作る。
    /// </summary>
    /// <param name="rules">判定の規則。</param>
    /// <param name="repository">異常の一覧の保存先。</param>
    /// <param name="settings">異常の設定。</param>
    /// <param name="notifications">通知を出すべきかの決め方。</param>
    /// <param name="timeProvider">現在時刻の提供元。</param>
    /// <param name="persistInterval">続いている異常をマスタに書く間隔。省略時は <see cref="DefaultPersistInterval"/>。</param>
    /// <exception cref="ArgumentNullException"><paramref name="persistInterval"/> 以外の引数が null。</exception>
    /// <exception cref="ArgumentException">同じ種類の規則が 2 つ以上ある。</exception>
    public AnomalyDetector(
        IEnumerable<IAnomalyRule> rules,
        IAnomalyRepository repository,
        AnomalySettingsCache settings,
        AnomalyNotificationPolicy notifications,
        TimeProvider timeProvider,
        TimeSpan? persistInterval = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _rules = rules.ToArray();
        if (_rules.GroupBy(r => r.Kind).Any(g => g.Count() > 1))
        {
            throw new ArgumentException("同じ種類の規則は 1 つだけにしてください。", nameof(rules));
        }

        _repository = repository;
        _settings = settings;
        _notifications = notifications;
        _timeProvider = timeProvider;
        _persistInterval = persistInterval ?? DefaultPersistInterval;
    }

    /// <summary>異常が始まった・重くなった・終わったときに発生する。値を受け取ったスレッドから発生する。</summary>
    public event EventHandler<AnomalyNotice>? AnomalyChanged;

    /// <summary>登録している規則の種類を取得する。</summary>
    public IReadOnlyList<AnomalyKind> Kinds => _rules.Select(r => r.Kind).ToArray();

    /// <summary>
    /// 見張りを始める前に呼ぶ。前回 Helper が止まる前に終わらせられなかった異常を、最後に確かめた時刻で終わらせる。
    /// </summary>
    /// <returns>終わらせた数。マスタに書けなかった場合は 0。</returns>
    public int Start()
    {
        lock (_gate)
        {
            _open.Clear();
            try
            {
                return _repository.CloseOngoing();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// カタログが変わったときに呼ぶ。部品の表示名（異常の一覧に残す名前）を覚える。
    /// </summary>
    /// <param name="catalog">新しいカタログ。</param>
    /// <exception cref="ArgumentNullException"><paramref name="catalog"/> が null。</exception>
    public void OnCatalogChanged(IReadOnlyList<SensorDescriptor> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var names = catalog
            .GroupBy(d => HardwareRecord.StableKeyOf(d.Key), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().HardwareName, StringComparer.Ordinal);
        lock (_gate)
        {
            _hardwareNames = names;
        }
    }

    /// <summary>
    /// 新しく読んだ値で判定する。Helper の収集の 1 回ごとに呼ぶ。
    /// </summary>
    /// <param name="readings">読んだ値。</param>
    /// <exception cref="ArgumentNullException"><paramref name="readings"/> が null。</exception>
    public void OnReadings(IReadOnlyList<SensorReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        var notices = new List<AnomalyNotice>();
        lock (_gate)
        {
            foreach (var reading in readings)
            {
                _latest[reading.Key] = reading;
            }

            var now = _timeProvider.GetUtcNow();
            foreach (var rule in _rules)
            {
                try
                {
                    var findings = rule.Evaluate(new AnomalyFrame(now, _latest, _settings.Get(rule.Kind)));
                    Apply(rule.Kind, findings, now, notices);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // 規則かマスタの失敗。この規則の今回の判定だけを飛ばす（続いている異常は終わらせない）。
                }
            }
        }

        foreach (var notice in notices)
        {
            AnomalyChanged?.Invoke(this, notice);
        }
    }

    /// <summary>
    /// 「これは正常」の印が変わったときに呼ぶ。続いている異常なら、メモリの状態にも反映する（以後の通知の判断に使う）。
    /// </summary>
    /// <param name="anomaly">変えたあとの異常。</param>
    /// <exception cref="ArgumentNullException"><paramref name="anomaly"/> が null。</exception>
    public void OnDismissalChanged(AnomalyRecord anomaly)
    {
        ArgumentNullException.ThrowIfNull(anomaly);
        Refresh(anomaly.Kind, anomaly.HardwareKey, anomaly.AnomalyId, r => r with { DismissedUtc = anomaly.DismissedUtc });
    }

    /// <summary>
    /// 通知を出したと知らされたときに呼ぶ。続いている異常なら、メモリの状態にも反映する（同じ異常を何度も通知しないため）。
    /// </summary>
    /// <param name="anomaly">通知を出した異常。</param>
    /// <param name="notifiedUtc">通知を出した時刻。</param>
    /// <exception cref="ArgumentNullException"><paramref name="anomaly"/> が null。</exception>
    public void OnNotified(AnomalyRecord anomaly, DateTimeOffset notifiedUtc)
    {
        ArgumentNullException.ThrowIfNull(anomaly);
        Refresh(anomaly.Kind, anomaly.HardwareKey, anomaly.AnomalyId, r => r with { NotifiedUtc = notifiedUtc });
    }

    private void Refresh(AnomalyKind kind, string hardwareKey, long anomalyId, Func<AnomalyRecord, AnomalyRecord> update)
    {
        lock (_gate)
        {
            if (_open.TryGetValue((kind, hardwareKey), out var open) && open.Record.AnomalyId == anomalyId)
            {
                open.Record = update(open.Record);
            }
        }
    }

    private void Apply(AnomalyKind kind, IReadOnlyList<AnomalyFinding> findings, DateTimeOffset now, List<AnomalyNotice> notices)
    {
        var active = new HashSet<string>(StringComparer.Ordinal);
        foreach (var finding in findings)
        {
            if (!active.Add(finding.HardwareKey))
            {
                continue;
            }

            if (_open.TryGetValue((kind, finding.HardwareKey), out var open))
            {
                Continue(kind, open, finding, now, notices);
            }
            else
            {
                Begin(kind, finding, now, notices);
            }
        }

        foreach (var (key, open) in _open.Where(o => o.Key.Kind == kind && !active.Contains(o.Key.HardwareKey)).ToArray())
        {
            var record = open.Record;
            _repository.End(record.AnomalyId, now, record.Severity, record.Evidence);
            _open.Remove(key);
            notices.Add(new AnomalyNotice(AnomalyChange.Ended, record with { LastSeenUtc = now, EndedUtc = now }, ShouldNotify: false));
        }
    }

    private void Begin(AnomalyKind kind, AnomalyFinding finding, DateTimeOffset now, List<AnomalyNotice> notices)
    {
        var started = finding.SinceUtc is { } since && since <= now ? since : now;
        var name = _hardwareNames.GetValueOrDefault(finding.HardwareKey, finding.HardwareKey);
        var record = _repository.Insert(new AnomalyRecord(
            0, kind, finding.HardwareKey, finding.HardwareKind, name, started, now, null, finding.Severity, finding.Evidence, null, null));
        _open[(kind, finding.HardwareKey)] = new OpenAnomaly(record, now, finding.Notifiable);
        notices.Add(new AnomalyNotice(AnomalyChange.Started, record, finding.Notifiable && ShouldNotify(kind, record, now)));
    }

    private void Continue(AnomalyKind kind, OpenAnomaly open, AnomalyFinding finding, DateTimeOffset now, List<AnomalyNotice> notices)
    {
        // 通知の対象でなかった異常が対象になったときも、重くなったときと同じく知らせる（通知を出す機会にする）。
        var becameNotifiable = finding.Notifiable && !open.Notifiable;
        open.Notifiable |= finding.Notifiable;
        var escalated = finding.Severity > open.Record.Severity;
        open.Record = open.Record with
        {
            LastSeenUtc = now,
            Severity = escalated ? finding.Severity : open.Record.Severity,
            Evidence = finding.Evidence,
        };

        if (escalated || now - open.PersistedUtc >= _persistInterval)
        {
            _repository.UpdateOngoing(open.Record.AnomalyId, now, open.Record.Severity, open.Record.Evidence);
            open.PersistedUtc = now;
        }

        if (escalated || becameNotifiable)
        {
            notices.Add(new AnomalyNotice(AnomalyChange.Escalated, open.Record, open.Notifiable && ShouldNotify(kind, open.Record, now)));
        }
    }

    private bool ShouldNotify(AnomalyKind kind, AnomalyRecord record, DateTimeOffset now) =>
        _settings.GetNotificationsEnabled()
        && _notifications.ShouldNotify(_settings.Get(kind), record, _repository.GetLastNotified(kind, record.HardwareKey), now);
}
