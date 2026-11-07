namespace Yamori.Core.Sensors.Providers.WindowsStandard;

/// <summary>
/// ハードウェア 1 つ分の <see cref="SensorSample"/> を、キーと説明をそろえて組み立てる。
/// </summary>
public sealed class SensorSampleBuilder
{
    private readonly HardwareKind _kind;
    private readonly string _hardwareId;
    private readonly string _hardwareName;
    private readonly List<SensorSample> _samples = [];

    /// <summary>
    /// ハードウェアを指定して作る。
    /// </summary>
    /// <param name="kind">ハードウェアの種類。</param>
    /// <param name="hardwareId">ハードウェアの ID（<see cref="HardwareIds"/> で作ったもの）。</param>
    /// <param name="hardwareName">ハードウェアの表示名。</param>
    /// <exception cref="ArgumentNullException"><paramref name="hardwareId"/> か <paramref name="hardwareName"/> が null。</exception>
    public SensorSampleBuilder(HardwareKind kind, string hardwareId, string hardwareName)
    {
        ArgumentNullException.ThrowIfNull(hardwareId);
        ArgumentNullException.ThrowIfNull(hardwareName);
        _kind = kind;
        _hardwareId = hardwareId;
        _hardwareName = hardwareName;
    }

    /// <summary>組み立てたセンサーを、追加した順に取得する。</summary>
    public IReadOnlyList<SensorSample> Samples => _samples;

    /// <summary>
    /// センサーを 1 つ追加する。
    /// </summary>
    /// <param name="type">量の種類。単位はこれで決まる。</param>
    /// <param name="name">ハードウェアの中でセンサーを区別する名前。</param>
    /// <param name="displayName">表示名。</param>
    /// <param name="value">値（基準単位）。測れなかった場合は null。</param>
    /// <param name="group">ツリーのグループ。省略時は量の種類の表記。</param>
    /// <returns>このインスタンス。</returns>
    public SensorSampleBuilder Add(SensorType type, string name, string displayName, double? value, string? group = null)
    {
        var key = SensorKey.Create(_kind, _hardwareId, type, name);
        var descriptor = new SensorDescriptor(
            key,
            SensorUnits.ForType(type),
            displayName,
            _hardwareName,
            group ?? SensorKeyTokens.ToToken(type),
            SensorProviderIds.WindowsStandard);
        _samples.Add(new SensorSample(descriptor, value is { } v && double.IsFinite(v) ? v : null));
        return this;
    }
}
