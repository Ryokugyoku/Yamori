namespace Yamori.Core.Sensors;

/// <summary>
/// センサーが測る量の種類。<see cref="SensorKey"/> の 3 つ目の区切りになり、基準単位もこれで決まる（<see cref="SensorUnits.ForType"/>）。
/// </summary>
/// <remarks>
/// IPC で数値として送るため、既存の値の番号は変えないこと。追加は末尾に行う。
/// </remarks>
public enum SensorType
{
    /// <summary>使用率（%）。キーの表記は <c>load</c>。</summary>
    Load = 0,

    /// <summary>動作クロック（Hz）。キーの表記は <c>clock</c>。</summary>
    Clock = 1,

    /// <summary>温度（℃）。キーの表記は <c>temperature</c>。</summary>
    Temperature = 2,

    /// <summary>電力（W）。キーの表記は <c>power</c>。</summary>
    Power = 3,

    /// <summary>電圧（V）。キーの表記は <c>voltage</c>。</summary>
    Voltage = 4,

    /// <summary>電流（A）。キーの表記は <c>current</c>。</summary>
    Current = 5,

    /// <summary>ファンの回転数（RPM）。キーの表記は <c>fan</c>。</summary>
    Fan = 6,

    /// <summary>ファンなどの制御の割合（%）。キーの表記は <c>control</c>。</summary>
    Control = 7,

    /// <summary>データ量（バイト）。キーの表記は <c>data</c>。</summary>
    Data = 8,

    /// <summary>転送の速さ（バイト／秒）。キーの表記は <c>throughput</c>。</summary>
    Throughput = 9,

    /// <summary>1 秒あたりの回数（/s）。キーの表記は <c>rate</c>。</summary>
    Rate = 10,

    /// <summary>個数。キーの表記は <c>count</c>。</summary>
    Count = 11,

    /// <summary>時間（秒）。キーの表記は <c>duration</c>。</summary>
    Duration = 12,

    /// <summary>使用率以外の割合（%。残量、性能の割合など）。キーの表記は <c>level</c>。</summary>
    Level = 13,

    /// <summary>エネルギー量（Wh）。キーの表記は <c>energy</c>。</summary>
    Energy = 14,

    /// <summary>オン・オフの印（0 か 1。熱や電力による制限がかかっているかなど）。キーの表記は <c>flag</c>。</summary>
    Flag = 15,
}
