namespace Yamori.Core.Hardware.Wmi;

/// <summary>
/// WMI クエリの実行に失敗したことを表す例外。
/// </summary>
public sealed class WmiQueryException : Exception
{
    /// <summary>
    /// 既定のメッセージで例外を作る。
    /// </summary>
    public WmiQueryException()
        : base("WMI クエリの実行に失敗しました。")
    {
    }

    /// <summary>
    /// メッセージを指定して例外を作る。
    /// </summary>
    /// <param name="message">エラーの説明。</param>
    public WmiQueryException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// メッセージと原因の例外を指定して例外を作る。
    /// </summary>
    /// <param name="message">エラーの説明。</param>
    /// <param name="innerException">原因になった例外。</param>
    public WmiQueryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
