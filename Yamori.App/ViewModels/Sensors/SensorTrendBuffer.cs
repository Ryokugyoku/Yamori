using System;
using System.Collections.Generic;
using Yamori.Core.Sensors;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// センサーごとに、直近の値をいくつか覚えておく（概要画面の折れ線グラフ用）。同じ時刻の値は二重に入れない。
/// 読めなかった値は入れない（グラフが途切れずに、読めた値だけでつながる）。
/// </summary>
public sealed class SensorTrendBuffer
{
    /// <summary>標準で覚えておく値の数（1 秒ごとなら 1 分ぶん）。</summary>
    public const int DefaultCapacity = 60;

    private readonly Dictionary<SensorKey, Entry> _entries = [];

    /// <summary>
    /// 覚えておく値の数を指定して作る。
    /// </summary>
    /// <param name="capacity">センサーごとに覚えておく値の数。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> が 2 未満。</exception>
    public SensorTrendBuffer(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 2);
        Capacity = capacity;
    }

    /// <summary>センサーごとに覚えておく値の数を取得する。</summary>
    public int Capacity { get; }

    /// <summary>
    /// 新しい値を加える。値がないか、前に加えた値より古い時刻なら加えない。
    /// </summary>
    /// <param name="reading">読んだ値。</param>
    /// <exception cref="ArgumentNullException"><paramref name="reading"/> が null。</exception>
    public void Add(SensorReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        if (reading.Value is not { } value || double.IsNaN(value) || double.IsInfinity(value))
        {
            return;
        }

        if (!_entries.TryGetValue(reading.Key, out var entry))
        {
            entry = new Entry();
            _entries[reading.Key] = entry;
        }

        if (reading.TimestampUtc <= entry.LastTimestampUtc)
        {
            return;
        }

        entry.LastTimestampUtc = reading.TimestampUtc;
        entry.Values.Enqueue(value);
        while (entry.Values.Count > Capacity)
        {
            entry.Values.Dequeue();
        }
    }

    /// <summary>
    /// 覚えている値を古い順に取得する。毎回新しい配列を返す（画面の更新の合図にするため）。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <returns>古い順の値。なければ空。</returns>
    public IReadOnlyList<double> Get(SensorKey key) =>
        _entries.TryGetValue(key, out var entry) ? entry.Values.ToArray() : [];

    /// <summary>
    /// 指定したキー以外の値を忘れる（カタログから消えたセンサーの分）。
    /// </summary>
    /// <param name="keep">残すキー。</param>
    /// <exception cref="ArgumentNullException"><paramref name="keep"/> が null。</exception>
    public void Retain(IReadOnlySet<SensorKey> keep)
    {
        ArgumentNullException.ThrowIfNull(keep);
        foreach (var key in new List<SensorKey>(_entries.Keys))
        {
            if (!keep.Contains(key))
            {
                _entries.Remove(key);
            }
        }
    }

    /// <summary>センサー 1 つ分の覚えている値。</summary>
    private sealed class Entry
    {
        /// <summary>古い順の値を取得する。</summary>
        public Queue<double> Values { get; } = new();

        /// <summary>最後に加えた値の時刻を取得または設定する。</summary>
        public DateTimeOffset LastTimestampUtc { get; set; } = DateTimeOffset.MinValue;
    }
}
