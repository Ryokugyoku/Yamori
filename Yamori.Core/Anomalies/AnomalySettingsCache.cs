using Yamori.Core.Anomalies.Storage;

namespace Yamori.Core.Anomalies;

/// <summary>
/// 異常の設定を、マスタから読んでメモリに持つ。検出は毎秒これを読み、変更はマスタに書いてから差し替える。スレッドセーフ。
/// </summary>
public sealed class AnomalySettingsCache
{
    private readonly IAnomalySettingsRepository _repository;
    private readonly Lock _lock = new();
    private IReadOnlyDictionary<AnomalyKind, AnomalySetting>? _settings;
    private bool? _notificationsEnabled;

    /// <summary>
    /// 保存先を指定して作る。読むのは最初に使うとき。
    /// </summary>
    /// <param name="repository">設定の保存先。</param>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> が null。</exception>
    public AnomalySettingsCache(IAnomalySettingsRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>
    /// すべての種類の設定を取得する。
    /// </summary>
    /// <returns>種類の順の設定。</returns>
    public IReadOnlyList<AnomalySetting> GetAll()
    {
        var settings = Load();
        return AnomalySettingRules.Kinds.Select(k => settings[k]).ToArray();
    }

    /// <summary>
    /// 種類の設定を取得する。
    /// </summary>
    /// <param name="kind">異常の種類。</param>
    /// <returns>設定。知らない種類なら初期の設定。</returns>
    public AnomalySetting Get(AnomalyKind kind) =>
        Load().TryGetValue(kind, out var setting) ? setting : AnomalySettingRules.Default(kind);

    /// <summary>
    /// 異常の通知の全体のオン・オフを取得する。オフなら、種類ごとの設定にかかわらず通知しない（異常は一覧に残す）。
    /// </summary>
    /// <returns>通知を出すなら true。</returns>
    public bool GetNotificationsEnabled()
    {
        lock (_lock)
        {
            return _notificationsEnabled ??= _repository.GetNotificationsEnabled();
        }
    }

    /// <summary>
    /// 異常の通知の全体のオン・オフをマスタに保存し、メモリの値を差し替える。
    /// </summary>
    /// <param name="enabled">通知を出すなら true。</param>
    public void SaveNotificationsEnabled(bool enabled)
    {
        lock (_lock)
        {
            _repository.SaveNotificationsEnabled(enabled);
            _notificationsEnabled = enabled;
        }
    }

    /// <summary>
    /// 設定をマスタに保存し、メモリの設定を差し替える。
    /// </summary>
    /// <param name="setting">設定。<see cref="AnomalySettingRules.IsValid"/> で確かめたもの。</param>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> が null。</exception>
    /// <exception cref="ArgumentException">設定の値が範囲の外。</exception>
    public void Save(AnomalySetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);
        if (!AnomalySettingRules.IsValid(setting))
        {
            throw new ArgumentException("異常の設定の値が範囲の外です。", nameof(setting));
        }

        lock (_lock)
        {
            _repository.Save(setting);
            var next = new Dictionary<AnomalyKind, AnomalySetting>(Load()) { [setting.Kind] = setting };
            _settings = next;
        }
    }

    private IReadOnlyDictionary<AnomalyKind, AnomalySetting> Load()
    {
        lock (_lock)
        {
            return _settings ??= _repository.GetAll().ToDictionary(s => s.Kind);
        }
    }
}
