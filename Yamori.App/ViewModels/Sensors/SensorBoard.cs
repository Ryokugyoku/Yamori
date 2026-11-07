using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Display;
using Yamori.Core.Sensors.Service;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// 画面で共有する、センサーの今の状態（カタログ、最新値、ヒステリシス付きの表示の状態）。
/// センサーの窓口の通知を受け取り、UI スレッドで状態を更新して <see cref="Updated"/> を発生させる。
/// </summary>
/// <remarks>UI スレッドで作り、UI スレッドから使うこと。</remarks>
public sealed class SensorBoard : IDisposable
{
    private readonly ISensorService _service;
    private readonly SynchronizationContext _uiContext;
    private readonly SensorVisibilityTracker _visibility = new();
    private readonly Dictionary<SensorKey, SensorReading> _latest = [];
    private IReadOnlyList<SensorDescriptor> _catalog = [];

    /// <summary>
    /// センサーの窓口を指定して作る。
    /// </summary>
    /// <param name="service">センサーの窓口。</param>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> が null。</exception>
    /// <exception cref="InvalidOperationException">UI スレッド以外から呼ばれた。</exception>
    public SensorBoard(ISensorService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException("SensorBoard は UI スレッドで作成してください。");

        _service.CatalogChanged += OnCatalogChanged;
        _service.ReadingsUpdated += OnReadingsUpdated;
        _ = LoadCatalogAsync();
    }

    /// <summary>
    /// 状態が変わったときに UI スレッドで発生する。
    /// </summary>
    public event EventHandler? Updated;

    /// <summary>今のカタログを取得する。</summary>
    public IReadOnlyList<SensorDescriptor> Catalog => _catalog;

    /// <summary>センサーの窓口を取得する。</summary>
    public ISensorService Service => _service;

    /// <summary>
    /// 表示の状態を取得する。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <returns>表示の状態。</returns>
    public SensorDisplayState GetState(SensorKey key) => _visibility.GetState(key);

    /// <summary>
    /// 表示するセンサーのキーを取得する。
    /// </summary>
    /// <returns>表示するキー。</returns>
    public IReadOnlyList<SensorKey> VisibleKeys() => _visibility.VisibleKeys();

    /// <summary>
    /// 最新値を取得する。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <returns>最新値。まだなければ null。</returns>
    public SensorReading? GetLatest(SensorKey key) => _latest.GetValueOrDefault(key);

    /// <summary>
    /// 説明を取得する。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <returns>説明。カタログになければ null。</returns>
    public SensorDescriptor? GetDescriptor(SensorKey key) => _catalog.FirstOrDefault(d => d.Key == key);

    /// <summary>
    /// 通知の購読を解除する。
    /// </summary>
    public void Dispose()
    {
        _service.CatalogChanged -= OnCatalogChanged;
        _service.ReadingsUpdated -= OnReadingsUpdated;
    }

    private async System.Threading.Tasks.Task LoadCatalogAsync()
    {
        var catalog = await _service.GetCatalogAsync();
        if (_catalog.Count == 0 && catalog.Count > 0)
        {
            ApplyCatalog(catalog);
        }
    }

    private void OnCatalogChanged(object? sender, IReadOnlyList<SensorDescriptor> catalog) =>
        _uiContext.Post(_ => ApplyCatalog(catalog), null);

    private void OnReadingsUpdated(object? sender, IReadOnlyList<SensorReading> readings) =>
        _uiContext.Post(_ => ApplyReadings(readings), null);

    private void ApplyCatalog(IReadOnlyList<SensorDescriptor> catalog)
    {
        _catalog = catalog;
        _visibility.SetCatalog(catalog.Select(d => d.Key));
        var keys = catalog.Select(d => d.Key).ToHashSet();
        foreach (var key in _latest.Keys.Where(k => !keys.Contains(k)).ToArray())
        {
            _latest.Remove(key);
        }

        Updated?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyReadings(IReadOnlyList<SensorReading> readings)
    {
        foreach (var reading in readings)
        {
            _visibility.Apply(reading);
            _latest[reading.Key] = reading;
        }

        Updated?.Invoke(this, EventArgs.Empty);
    }
}
