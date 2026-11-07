using Yamori.Core.Hardware.DeviceIo;
using Yamori.Core.Hardware.Graphics;
using Yamori.Core.Hardware.Smbios;
using Yamori.Core.Hardware.Topology;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// 決まったバイト列を SMBIOS テーブルとして返す、テスト用の情報源。
/// </summary>
/// <param name="raw">RawSMBIOSData のバイト列。null の場合はテーブルがないものとする。</param>
internal sealed class FakeSmbiosTableSource(byte[]? raw) : ISmbiosTableSource
{
    /// <inheritdoc />
    public SmbiosTable? Read() => raw is null ? null : new SmbiosTableParser().Parse(raw);
}

/// <summary>
/// 決まった GPU の一覧を返す、テスト用の情報源。
/// </summary>
/// <param name="adapters">返す GPU の一覧。</param>
internal sealed class FakeGraphicsAdapterSource(params GraphicsAdapterDetails[] adapters) : IGraphicsAdapterSource
{
    /// <inheritdoc />
    public IReadOnlyList<GraphicsAdapterDetails> GetAdapters() => adapters;
}

/// <summary>
/// ディスクの番号ごとに決まった温度を返す、テスト用の情報源。
/// </summary>
/// <param name="temperatures">ディスクの番号と温度（℃）の組。含まれない番号は null を返す。</param>
internal sealed class FakeStorageTemperatureSource(IReadOnlyDictionary<int, int>? temperatures = null) : IStorageTemperatureSource
{
    /// <summary>問い合わせを受けたディスクの番号を取得する。</summary>
    public List<int> Requested { get; } = [];

    /// <inheritdoc />
    public int? ReadCelsius(int diskNumber)
    {
        Requested.Add(diskNumber);
        return temperatures is not null && temperatures.TryGetValue(diskNumber, out var celsius) ? celsius : null;
    }
}

/// <summary>
/// 決まったコアの内訳を返す、テスト用の情報源。
/// </summary>
/// <param name="topology">返す内訳。</param>
internal sealed class FakeCoreTopologySource(CoreTopology? topology) : ICoreTopologySource
{
    /// <inheritdoc />
    public CoreTopology? Read() => topology;
}
