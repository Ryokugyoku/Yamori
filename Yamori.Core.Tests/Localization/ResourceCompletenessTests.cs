using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Yamori.Core.Tests.Localization;

/// <summary>
/// リポジトリ内のすべての .resx について、日本語・スペイン語の翻訳漏れと、書式の埋め込み位置の食い違いがないことを確かめる。
/// </summary>
public partial class ResourceCompletenessTests
{
    private static readonly string[] TranslatedCultures = ["ja", "es"];

    public static TheoryData<string, string> TranslationFiles()
    {
        var data = new TheoryData<string, string>();
        foreach (var neutral in Directory.EnumerateFiles(FindRepositoryRoot(), "*.resx", SearchOption.AllDirectories)
                     .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                     .Where(p => Path.GetFileNameWithoutExtension(p).IndexOf('.') < 0))
        {
            foreach (var culture in TranslatedCultures)
            {
                data.Add(neutral, culture);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(TranslationFiles))]
    public void EveryKeyIsTranslatedWithTheSamePlaceholders(string neutralPath, string culture)
    {
        var translatedPath = Path.ChangeExtension(neutralPath, null) + "." + culture + ".resx";
        Assert.True(File.Exists(translatedPath), $"{translatedPath} がありません。");

        var neutral = Load(neutralPath);
        var translated = Load(translatedPath);

        Assert.Equal(neutral.Keys.Order(), translated.Keys.Order());
        foreach (var (key, value) in neutral)
        {
            Assert.False(string.IsNullOrWhiteSpace(translated[key]), $"{culture}: {key} が空です。");
            Assert.True(
                Placeholders(value).SetEquals(Placeholders(translated[key])),
                $"{culture}: {key} の埋め込み位置が英語版と違います。");
        }
    }

    [Fact]
    public void ResourceFilesAreFound()
    {
        Assert.True(TranslationFiles().Count() >= TranslatedCultures.Length);
    }

    private static Dictionary<string, string> Load(string path) =>
        XDocument.Load(path).Root!.Elements("data")
            .ToDictionary(e => (string)e.Attribute("name")!, e => (string?)e.Element("value") ?? string.Empty);

    private static HashSet<string> Placeholders(string value) =>
        PlaceholderPattern().Matches(value).Select(m => m.Groups[1].Value).ToHashSet();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Yamori.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Yamori.slnx が見つかりません。");
    }

    [GeneratedRegex(@"\{(\d+)(?:[:,][^}]*)?\}")]
    private static partial Regex PlaceholderPattern();
}
