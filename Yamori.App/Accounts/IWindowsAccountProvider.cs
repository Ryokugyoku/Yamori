namespace Yamori.Accounts;

/// <summary>
/// サインイン中の Windows アカウントを取得する。
/// </summary>
public interface IWindowsAccountProvider
{
    /// <summary>
    /// サインイン中の Windows アカウントを取得する。
    /// </summary>
    /// <returns>サインイン中のアカウント。</returns>
    WindowsAccount GetCurrent();
}
