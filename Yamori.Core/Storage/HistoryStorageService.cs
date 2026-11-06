using Yamori.Core.Settings;

namespace Yamori.Core.Storage;

/// <summary>
/// 設定に従って記録の保存先を決め、変更されたら履歴をコピーして保存先を切り替える。
/// </summary>
public sealed class HistoryStorageService
{
    private readonly HistoryLocationResolver _resolver;
    private readonly HistoryMigrator _migrator;
    private readonly Func<string, IUsageHistoryStore> _storeFactory;
    private readonly SwitchableUsageHistoryStore _store;
    private readonly Lock _sync = new();
    private HistoryLocation _currentLocation;

    /// <summary>
    /// 必要な処理と最初の設定を指定して作る。選んだ保存先が使えなければ、この PC の中に保存する。
    /// </summary>
    /// <param name="resolver">設定から保存先を決める処理。</param>
    /// <param name="migrator">履歴をコピーする処理。</param>
    /// <param name="storeFactory">フォルダーを受け取って履歴ストアを作る処理。</param>
    /// <param name="initialSettings">起動時の設定。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    /// <exception cref="IOException">この PC の中の保存先も使えない。</exception>
    public HistoryStorageService(
        HistoryLocationResolver resolver,
        HistoryMigrator migrator,
        Func<string, IUsageHistoryStore> storeFactory,
        YamoriSettings initialSettings)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(migrator);
        ArgumentNullException.ThrowIfNull(storeFactory);
        ArgumentNullException.ThrowIfNull(initialSettings);

        _resolver = resolver;
        _migrator = migrator;
        _storeFactory = storeFactory;

        var location = resolver.Resolve(initialSettings);
        IUsageHistoryStore inner;
        try
        {
            inner = storeFactory(location.Directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            location = resolver.GetLocal(isFallback: true);
            inner = storeFactory(location.Directory);
        }

        _currentLocation = location;
        _store = new SwitchableUsageHistoryStore(inner);
    }

    /// <summary>
    /// 保存先が変わったときに発生する。UI スレッド以外から呼ばれることがある。
    /// </summary>
    public event EventHandler<HistoryLocation>? LocationChanged;

    /// <summary>
    /// 監視処理やレポート処理に渡す履歴ストアを取得する。保存先を切り替えても同じインスタンスのまま。
    /// </summary>
    public IUsageHistoryStore Store => _store;

    /// <summary>
    /// 今の保存先を取得する。
    /// </summary>
    public HistoryLocation CurrentLocation
    {
        get
        {
            lock (_sync)
            {
                return _currentLocation;
            }
        }
    }

    /// <summary>
    /// 設定に従って保存先を切り替える。これまでの履歴は新しい保存先へコピーする（元の履歴は消さない）。
    /// ファイルの読み書きを伴うため、UI スレッド以外から呼ぶこと。
    /// </summary>
    /// <param name="settings">新しい設定。</param>
    /// <returns>切り替えた後の保存先。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> が null。</exception>
    /// <exception cref="IOException">新しい保存先を作れない、または履歴をコピーできない。このときは切り替えない。</exception>
    /// <exception cref="UnauthorizedAccessException">新しい保存先に書き込む権限がない。このときは切り替えない。</exception>
    public HistoryLocation Apply(YamoriSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        HistoryLocation previous;
        HistoryLocation next;
        lock (_sync)
        {
            previous = _currentLocation;
            next = _resolver.Resolve(settings);
            if (!HistoryMigrator.IsSameDirectory(previous.Directory, next.Directory))
            {
                var store = _storeFactory(next.Directory);
                _store.SwitchTo(store, () => _migrator.Migrate(previous.Directory, next.Directory));
            }

            _currentLocation = next;
        }

        if (next != previous)
        {
            LocationChanged?.Invoke(this, next);
        }

        return next;
    }
}
