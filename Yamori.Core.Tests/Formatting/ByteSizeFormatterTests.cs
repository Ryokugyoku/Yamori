using Yamori.Core.Formatting;

namespace Yamori.Core.Tests.Formatting;

public class ByteSizeFormatterTests
{
    [Theory]
    [InlineData(0UL, "0 B")]
    [InlineData(1023UL, "1023 B")]
    [InlineData(1536UL, "1.5 KB")]
    [InlineData(16UL << 30, "16 GB")]
    [InlineData(1UL << 40, "1 TB")]
    [InlineData(512UL << 20, "512 MB")]
    public void FormatsWithBinaryUnits(ulong bytes, string expected)
    {
        Assert.Equal(expected, ByteSizeFormatter.Format(bytes));
    }
}
