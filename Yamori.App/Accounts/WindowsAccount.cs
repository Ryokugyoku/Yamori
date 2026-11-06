namespace Yamori.Accounts;

/// <summary>
/// Yamori を使っている Windows アカウント。
/// </summary>
/// <param name="UserName">ユーザー名。</param>
/// <param name="DomainName">ドメイン名、またはローカルアカウントのときは PC 名。</param>
public sealed record WindowsAccount(string UserName, string DomainName)
{
    /// <summary>
    /// <c>ドメイン\ユーザー名</c> の形式の名前を取得する。
    /// </summary>
    public string QualifiedName => $"{DomainName}\\{UserName}";
}
