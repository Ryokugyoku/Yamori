using System.Diagnostics;

namespace Yamori.Core.Sensors.Extended.Signing;

/// <summary>
/// PawnIO のドライバーが公式版（Official Build）かどうかを、次の 2 つがどちらも満たされるかで確かめる。
/// </summary>
/// <remarks>
/// <list type="number">
/// <item>ドライバー（<c>PawnIO.sys</c>）の署名が WinVerifyTrust で正しく、署名者が <c>Microsoft Windows Hardware Compatibility Publisher</c>
/// （Microsoft のドライバー署名）であること。Unrestricted 版は署名されていない。</item>
/// <item>ファイルの説明（バージョン情報の FileDescription）が <c>PawnIO Official Build</c> であること（PawnIO のビルド設定の <c>PAWNIO_NAME_FULL</c> の既定値）。</item>
/// </list>
/// </remarks>
public sealed class PawnIoEditionVerifier : IPawnIoEditionVerifier
{
    /// <summary>公式版のドライバーのファイルの説明。</summary>
    public const string OfficialFileDescription = "PawnIO Official Build";

    /// <summary>Microsoft のドライバー署名の署名者の名前（CN）。</summary>
    public const string MicrosoftDriverSignerName = "CN=Microsoft Windows Hardware Compatibility Publisher";

    private const string ServiceName = "PawnIO";

    /// <inheritdoc />
    public bool IsOfficial()
    {
        var path = DriverImagePathResolver.TryResolve(ServiceName);
        if (path is null || !File.Exists(path))
        {
            return false;
        }

        return IsMicrosoftDriverSigner(CatalogSignatureVerifier.TryGetVerifiedSigner(path))
            && IsOfficialDescription(FileVersionInfo.GetVersionInfo(path).FileDescription);
    }

    /// <summary>
    /// 署名者が Microsoft のドライバー署名かどうかを調べる。
    /// </summary>
    /// <param name="signerSubject">確かめた署名者のサブジェクト。確かめられなかった場合は null。</param>
    /// <returns>Microsoft のドライバー署名なら true。</returns>
    public static bool IsMicrosoftDriverSigner(string? signerSubject) =>
        signerSubject is not null
        && signerSubject.Split(',', StringSplitOptions.TrimEntries).Contains(MicrosoftDriverSignerName, StringComparer.Ordinal);

    /// <summary>
    /// ファイルの説明が公式版のものかどうかを調べる。
    /// </summary>
    /// <param name="fileDescription">ファイルの説明。</param>
    /// <returns>公式版の説明なら true。</returns>
    public static bool IsOfficialDescription(string? fileDescription) =>
        string.Equals(fileDescription?.Trim(), OfficialFileDescription, StringComparison.Ordinal);
}
