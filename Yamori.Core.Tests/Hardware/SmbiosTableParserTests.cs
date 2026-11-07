using Yamori.Core.Hardware.Smbios;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Hardware;

public class SmbiosTableParserTests
{
    [Fact]
    public void ParsesStructuresStringsAndVersion()
    {
        var raw = SmbiosBytes.Build(
            ([2, 0x08, 0x10, 0x00, 0x01, 0x02, 0x00, 0x00], ["Board Maker", "Board Name"]),
            ([27, 0x0F, 0x20, 0x00, 0, 0, 0x63, 0, 0, 0, 0, 0, 0, 0, 0], []));

        var table = new SmbiosTableParser().Parse(raw)!;

        Assert.Equal((3, 4), (table.MajorVersion, table.MinorVersion));
        Assert.Equal([2, 27, 127], table.Structures.Select(s => (int)s.Type));
        var board = table.Structures[0];
        Assert.Equal(0x0010, board.Handle);
        Assert.Equal("Board Maker", board.GetString(0x04));
        Assert.Equal("Board Name", board.GetString(0x05));
        Assert.Null(board.GetString(0x06));
        Assert.Null(board.GetWord(0x07));
    }

    [Fact]
    public void StopsAtTruncatedData()
    {
        var raw = SmbiosBytes.Build(([2, 0x08, 0x10, 0x00, 0x01, 0x02, 0x00, 0x00], ["Board Maker"]));

        var table = new SmbiosTableParser().Parse(raw.AsSpan(0, raw.Length - 12))!;

        Assert.Single(table.Structures);
    }

    [Fact]
    public void ReturnsNullForTooShortData()
    {
        Assert.Null(new SmbiosTableParser().Parse([0, 3, 4]));
    }
}
