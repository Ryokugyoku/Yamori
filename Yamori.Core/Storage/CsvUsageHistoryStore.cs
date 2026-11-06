using System.Globalization;
using System.Text;
using Yamori.Core.Hardware;
using Yamori.Core.Monitoring;

namespace Yamori.Core.Storage;

/// <summary>
/// 使用率の履歴を、UTC の日付ごとの CSV ファイルとしてローカルに保存する。
/// </summary>
/// <remarks>
/// 1 日ごとにファイルを分けることで、古い履歴の削除や、必要な期間だけの読み込みを簡単にしている。
/// 同じインスタンスからの同時アクセスはロックで直列化する。
/// </remarks>
public sealed class CsvUsageHistoryStore : IUsageHistoryStore
{
    private const string FilePrefix = "usage-";
    private const string FileExtension = ".csv";

    private readonly string _directory;
    private readonly UsageSampleCsvSerializer _serializer;
    private readonly Lock _sync = new();

    /// <summary>
    /// 保存先のフォルダーと変換処理を指定して作る。フォルダーがなければ作る。
    /// </summary>
    /// <param name="directory">CSV ファイルを置くフォルダー。</param>
    /// <param name="serializer">サンプルと CSV 行の変換処理。</param>
    /// <exception cref="ArgumentException"><paramref name="directory"/> が空。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serializer"/> が null。</exception>
    public CsvUsageHistoryStore(string directory, UsageSampleCsvSerializer serializer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(serializer);

        _directory = directory;
        _serializer = serializer;
        Directory.CreateDirectory(_directory);
    }

    /// <inheritdoc />
    public void Append(IReadOnlyCollection<ComponentUsageSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        lock (_sync)
        {
            foreach (var group in samples.GroupBy(s => DateOnly.FromDateTime(s.Timestamp.UtcDateTime)))
            {
                var lines = group.Select(_serializer.Serialize);
                File.AppendAllLines(GetFilePath(group.Key), lines, Encoding.UTF8);
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ComponentUsageSample> Read(HardwareComponent component, DateTimeOffset from, DateTimeOffset to)
    {
        if (to < from)
        {
            return [];
        }

        var result = new List<ComponentUsageSample>();
        lock (_sync)
        {
            var first = DateOnly.FromDateTime(from.UtcDateTime);
            var last = DateOnly.FromDateTime(to.UtcDateTime);
            for (var day = first; day <= last; day = day.AddDays(1))
            {
                var path = GetFilePath(day);
                if (!File.Exists(path))
                {
                    continue;
                }

                foreach (var line in File.ReadLines(path, Encoding.UTF8))
                {
                    if (_serializer.TryDeserialize(line, out var sample)
                        && sample.Component == component
                        && sample.Timestamp >= from
                        && sample.Timestamp <= to)
                    {
                        result.Add(sample);
                    }
                }
            }
        }

        result.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        return result;
    }

    private string GetFilePath(DateOnly day) =>
        Path.Combine(_directory, FilePrefix + day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + FileExtension);
}
