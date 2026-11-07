namespace Yamori.Core.Hardware.Wmi;

/// <summary>
/// <see cref="IWmiQueryExecutor"/> の便利な使い方をまとめる。
/// </summary>
public static class WmiQueryExecutorExtensions
{
    /// <summary>
    /// WQL クエリを実行し、失敗した場合は空の結果を返す。機種によっては存在しないクラスを読むときに使う。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <param name="wmiNamespace">名前空間。</param>
    /// <param name="query">WQL クエリ。</param>
    /// <returns>結果の行。失敗した場合は空。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public static IReadOnlyList<WmiRecord> QueryOrEmpty(this IWmiQueryExecutor wmi, string wmiNamespace, string query)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        try
        {
            return wmi.Query(wmiNamespace, query);
        }
        catch (WmiQueryException)
        {
            // そのクラスを公開していない機種や、権限が足りない場合がある。他の情報の取得は続ける。
            return [];
        }
    }
}
