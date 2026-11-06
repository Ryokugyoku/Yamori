using System;
using System.Threading.Tasks;
using Windows.Security.Credentials.UI;

namespace Yamori.Accounts;

/// <summary>
/// Windows Hello（顔、指紋、PIN）で本人確認を行う。確認は Windows が PC の中だけで行い、ネットワークは使わない。
/// </summary>
public sealed class WindowsHelloAccountVerifier : IAccountVerifier
{
    private readonly Func<nint> _getWindowHandle;

    /// <summary>
    /// 確認画面の親にするウィンドウを指定して作る。
    /// </summary>
    /// <param name="getWindowHandle">親ウィンドウのハンドルを返す処理。</param>
    /// <exception cref="ArgumentNullException"><paramref name="getWindowHandle"/> が null。</exception>
    public WindowsHelloAccountVerifier(Func<nint> getWindowHandle)
    {
        ArgumentNullException.ThrowIfNull(getWindowHandle);
        _getWindowHandle = getWindowHandle;
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync() =>
        await UserConsentVerifier.CheckAvailabilityAsync() == UserConsentVerifierAvailability.Available;

    /// <inheritdoc />
    public async Task<AccountVerificationResult> VerifyAsync(string message)
    {
        if (!await IsAvailableAsync())
        {
            return AccountVerificationResult.Unavailable;
        }

        // デスクトップアプリでは、確認画面をこのウィンドウの前面に出すため Interop 版を使う。
        var result = await UserConsentVerifierInterop.RequestVerificationForWindowAsync(_getWindowHandle(), message);
        return result switch
        {
            UserConsentVerificationResult.Verified => AccountVerificationResult.Verified,
            UserConsentVerificationResult.Canceled => AccountVerificationResult.Canceled,
            UserConsentVerificationResult.DeviceNotPresent
                or UserConsentVerificationResult.NotConfiguredForUser
                or UserConsentVerificationResult.DisabledByPolicy => AccountVerificationResult.Unavailable,
            _ => AccountVerificationResult.Failed,
        };
    }
}
