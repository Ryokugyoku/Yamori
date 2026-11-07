using Yamori.Core.Hardware.Topology;

namespace Yamori.Core.Tests.Hardware;

public class CoreTopologyParserTests
{
    private const int RecordSize = 48;

    private static byte[] Cores(params byte[] efficiencyClasses) =>
        efficiencyClasses.SelectMany(efficiencyClass =>
        {
            var record = new byte[RecordSize];
            BitConverter.GetBytes(0u).CopyTo(record, 0);
            BitConverter.GetBytes((uint)RecordSize).CopyTo(record, 4);
            record[9] = efficiencyClass;
            return record;
        }).ToArray();

    [Fact]
    public void CountsPerformanceAndEfficiencyCores()
    {
        var topology = new CoreTopologyParser().Parse(Cores(1, 1, 0, 0, 0, 0));

        Assert.Equal(new CoreTopology(2, 4), topology);
    }

    [Fact]
    public void ReturnsNullWhenAllCoresAreTheSame()
    {
        Assert.Null(new CoreTopologyParser().Parse(Cores(0, 0, 0, 0)));
    }
}
