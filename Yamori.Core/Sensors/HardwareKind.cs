namespace Yamori.Core.Sensors;

/// <summary>
/// センサーが属するハードウェアの種類。<see cref="SensorKey"/> の 1 つ目の区切りになる。
/// </summary>
/// <remarks>
/// IPC で数値として送るため、既存の値の番号は変えないこと。追加は末尾に行う。
/// </remarks>
public enum HardwareKind
{
    /// <summary>CPU（パッケージ）。キーの表記は <c>cpu</c>。</summary>
    Cpu = 0,

    /// <summary>メインメモリ（システム全体）。キーの表記は <c>memory</c>。</summary>
    Memory = 1,

    /// <summary>GPU アダプター。キーの表記は <c>gpu</c>。</summary>
    Gpu = 2,

    /// <summary>物理ディスク（HDD・SSD）。キーの表記は <c>storage</c>。</summary>
    Storage = 3,

    /// <summary>ドライブ（ボリューム）。キーの表記は <c>volume</c>。</summary>
    Volume = 4,

    /// <summary>ネットワーク アダプター。キーの表記は <c>network</c>。</summary>
    Network = 5,

    /// <summary>バッテリー（システム全体）。キーの表記は <c>battery</c>。</summary>
    Battery = 6,

    /// <summary>マザーボード（Super I/O、EC など）。キーの表記は <c>motherboard</c>。</summary>
    Motherboard = 7,

    /// <summary>ACPI の温度センサー（サーマル ゾーン）。キーの表記は <c>thermal-zone</c>。</summary>
    ThermalZone = 8,

    /// <summary>Windows 全体（プロセス数、稼働時間など）。キーの表記は <c>system</c>。</summary>
    System = 9,

    /// <summary>電源ユニット。キーの表記は <c>psu</c>。</summary>
    PowerSupply = 10,
}
