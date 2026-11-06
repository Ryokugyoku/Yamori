namespace Yamori.Core.Hardware.Wmi;

/// <summary>
/// WMI／CIM に WQL クエリを投げて結果を返す。テストで差し替えられるように抽象化している。
/// </summary>
public interface IWmiQueryExecutor
{
    /// <summary>
    /// WQL クエリを実行する。
    /// </summary>
    /// <param name="wmiNamespace">名前空間（例：<c>root\cimv2</c>）。</param>
    /// <param name="query">WQL クエリ（例：<c>SELECT Name FROM Win32_Processor</c>）。</param>
    /// <returns>結果の行。該当がなければ空。</returns>
    /// <exception cref="WmiQueryException">クエリの実行に失敗した。</exception>
    IReadOnlyList<WmiRecord> Query(string wmiNamespace, string query);
}
