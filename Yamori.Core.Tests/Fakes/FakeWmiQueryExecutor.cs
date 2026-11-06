using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// クラス名ごとに決めた結果を返す、テスト用の WMI 実行器。
/// </summary>
internal sealed class FakeWmiQueryExecutor : IWmiQueryExecutor
{
    private readonly Dictionary<string, IReadOnlyList<WmiRecord>> _results = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _failingClasses = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// クラスに対する結果を登録する。
    /// </summary>
    /// <param name="wmiClass">WMI クラス名。</param>
    /// <param name="rows">返す行。各行はプロパティ名と値の組。</param>
    /// <returns>このインスタンス。</returns>
    public FakeWmiQueryExecutor With(string wmiClass, params Dictionary<string, object?>[] rows)
    {
        _results[wmiClass] = rows.Select(r => new WmiRecord(r)).ToArray();
        return this;
    }

    /// <summary>
    /// クラスへのクエリが失敗するようにする。
    /// </summary>
    /// <param name="wmiClass">WMI クラス名。</param>
    /// <returns>このインスタンス。</returns>
    public FakeWmiQueryExecutor Failing(string wmiClass)
    {
        _failingClasses.Add(wmiClass);
        return this;
    }

    /// <inheritdoc />
    public IReadOnlyList<WmiRecord> Query(string wmiNamespace, string query)
    {
        var wmiClass = query.Split(" FROM ", StringSplitOptions.TrimEntries)[^1];
        if (_failingClasses.Contains(wmiClass))
        {
            throw new WmiQueryException($"{wmiClass} は失敗するように設定されています。");
        }

        return _results.TryGetValue(wmiClass, out var rows) ? rows : [];
    }
}
