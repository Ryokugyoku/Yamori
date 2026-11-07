using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Registry;

namespace Yamori.Core.Tests.Sensors;

public sealed class SensorPriorityTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "yamori-tests", Guid.NewGuid().ToString("N"));

    public SensorPriorityTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void PolicyOrdersByRuleThenDefaultThenId()
    {
        var policy = new SensorPriorityPolicy(new SensorPriorityOptions(
            ["pawnio", "win-std"],
            [new SensorPriorityRule("gpu/*/*/*", ["win-std"])]));

        Assert.Equal(["pawnio", "win-std", "zzz"], policy.Order(SensorKey.Parse("cpu/0/load/total"), ["zzz", "win-std", "pawnio"]));
        Assert.Equal(["win-std", "pawnio"], policy.Order(SensorKey.Parse("gpu/abc/load/max-engine"), ["pawnio", "win-std"]));
    }

    [Fact]
    public void FirstMatchingRuleWins()
    {
        var policy = new SensorPriorityPolicy(new SensorPriorityOptions(
            ["pawnio", "win-std"],
            [
                new SensorPriorityRule("cpu/*/load/*", ["win-std"]),
                new SensorPriorityRule("cpu/*/*/*", ["pawnio"]),
            ]));

        Assert.Equal("win-std", policy.Order(SensorKey.Parse("cpu/0/load/total"), ["pawnio", "win-std"])[0]);
        Assert.Equal("pawnio", policy.Order(SensorKey.Parse("cpu/0/clock/base"), ["pawnio", "win-std"])[0]);
    }

    [Fact]
    public void LoaderReturnsDefaultWhenFileIsMissing()
    {
        var options = new SensorPriorityOptionsLoader(Path.Combine(_directory, "missing.json")).Load();

        Assert.Same(SensorPriorityOptions.Default, options);
    }

    [Fact]
    public void LoaderReadsRulesFromJson()
    {
        var path = WriteFile("""
            {
              // コメントも書ける
              "defaultOrder": ["win-std", "pawnio"],
              "rules": [ { "pattern": "gpu/*/*/*", "order": ["pawnio"] } ],
            }
            """);

        var options = new SensorPriorityOptionsLoader(path).Load();

        Assert.Equal(["win-std", "pawnio"], options.DefaultOrder);
        var rule = Assert.Single(options.Rules);
        Assert.Equal("gpu/*/*/*", rule.Pattern);
        Assert.Equal(["pawnio"], rule.Order);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "rules": [] }""")]
    [InlineData("""{ "defaultOrder": ["win-std"], "rules": [ { "pattern": "gpu/*", "order": [] } ] }""")]
    public void LoaderFallsBackToDefaultForInvalidFiles(string json)
    {
        var options = new SensorPriorityOptionsLoader(WriteFile(json)).Load();

        Assert.Same(SensorPriorityOptions.Default, options);
    }

    private string WriteFile(string content)
    {
        var path = Path.Combine(_directory, "sensor-priority.json");
        File.WriteAllText(path, content);
        return path;
    }
}
