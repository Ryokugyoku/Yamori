using System.Threading.Tasks;

namespace Yamori.Accounts;

/// <summary>
/// サインイン中の Windows アカウントの本人確認を行う。
/// </summary>
public interface IAccountVerifier
{
    /// <summary>
    /// 本人確認の仕組みが使えるかどうかを調べる。
    /// </summary>
    /// <returns>使えるなら true。</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// 本人確認を行う。
    /// </summary>
    /// <param name="message">確認画面に表示する説明。</param>
    /// <returns>確認の結果。</returns>
    Task<AccountVerificationResult> VerifyAsync(string message);
}
