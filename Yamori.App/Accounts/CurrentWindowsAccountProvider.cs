using System;

namespace Yamori.Accounts;

/// <summary>
/// プロセスを実行している Windows アカウントを返す。ネットワークは使わない。
/// </summary>
public sealed class CurrentWindowsAccountProvider : IWindowsAccountProvider
{
    /// <inheritdoc />
    public WindowsAccount GetCurrent() => new(Environment.UserName, Environment.UserDomainName);
}
