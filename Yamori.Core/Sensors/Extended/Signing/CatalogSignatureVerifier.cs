using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Yamori.Core.Sensors.Extended.Signing;

/// <summary>
/// カタログで署名されたファイル（PnP で入れたドライバーなど）の署名を、WinVerifyTrust で確かめる。ネットワークには出ない。
/// </summary>
public static class CatalogSignatureVerifier
{
    /// <summary>
    /// ファイルの署名を確かめ、署名者の証明書の名前を取得する。
    /// </summary>
    /// <param name="filePath">確かめるファイルのパス。</param>
    /// <returns>署名が正しければ署名者（連鎖の先頭の証明書）のサブジェクト。署名がない、または正しくない場合は null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="filePath"/> が null。</exception>
    public static string? TryGetVerifiedSigner(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        try
        {
            using var file = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var subsystem = WinTrustNative.DriverActionVerify;
            if (!WinTrustNative.CryptCATAdminAcquireContext2(out var catAdmin, ref subsystem, "SHA256", 0, 0))
            {
                return null;
            }

            try
            {
                var hash = CalculateHash(catAdmin, file);
                if (hash is null)
                {
                    return null;
                }

                var catalogPath = FindCatalog(catAdmin, hash);
                return catalogPath is null ? null : VerifyMember(catalogPath, filePath, file, hash, catAdmin);
            }
            finally
            {
                WinTrustNative.CryptCATAdminReleaseContext(catAdmin, 0);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static byte[]? CalculateHash(nint catAdmin, SafeFileHandle file)
    {
        uint size = 0;
        WinTrustNative.CryptCATAdminCalcHashFromFileHandle2(catAdmin, file, ref size, null, 0);
        if (size == 0)
        {
            return null;
        }

        var hash = new byte[size];
        return WinTrustNative.CryptCATAdminCalcHashFromFileHandle2(catAdmin, file, ref size, hash, 0) ? hash : null;
    }

    private static unsafe string? FindCatalog(nint catAdmin, byte[] hash)
    {
        nint previous = 0;
        var catalog = WinTrustNative.CryptCATAdminEnumCatalogFromHash(catAdmin, hash, (uint)hash.Length, 0, ref previous);
        if (catalog == 0)
        {
            return null;
        }

        try
        {
            var info = new WinTrustNative.CatalogInfo { CbStruct = (uint)sizeof(WinTrustNative.CatalogInfo) };
            if (!WinTrustNative.CryptCATCatalogInfoFromContext(catalog, ref info, 0))
            {
                return null;
            }

            return new string(info.CatalogFile);
        }
        finally
        {
            WinTrustNative.CryptCATAdminReleaseCatalogContext(catAdmin, catalog, 0);
        }
    }

    private static unsafe string? VerifyMember(string catalogPath, string filePath, SafeFileHandle file, byte[] hash, nint catAdmin)
    {
        var catalogPathPtr = Marshal.StringToHGlobalUni(catalogPath);
        var memberTagPtr = Marshal.StringToHGlobalUni(Convert.ToHexString(hash));
        var memberPathPtr = Marshal.StringToHGlobalUni(filePath);
        var hashPtr = Marshal.AllocHGlobal(hash.Length);
        var catalogInfoPtr = Marshal.AllocHGlobal(sizeof(WinTrustNative.WinTrustCatalogInfo));
        var added = false;
        try
        {
            file.DangerousAddRef(ref added);
            Marshal.Copy(hash, 0, hashPtr, hash.Length);
            var catalogInfo = new WinTrustNative.WinTrustCatalogInfo
            {
                CbStruct = (uint)sizeof(WinTrustNative.WinTrustCatalogInfo),
                CatalogFilePath = catalogPathPtr,
                MemberTag = memberTagPtr,
                MemberFilePath = memberPathPtr,
                MemberFile = file.DangerousGetHandle(),
                CalculatedFileHash = hashPtr,
                CalculatedFileHashSize = (uint)hash.Length,
                CatAdmin = catAdmin,
            };
            Marshal.StructureToPtr(catalogInfo, catalogInfoPtr, fDeleteOld: false);

            var data = new WinTrustNative.WinTrustData
            {
                CbStruct = (uint)sizeof(WinTrustNative.WinTrustData),
                UiChoice = WinTrustNative.UiNone,
                RevocationChecks = WinTrustNative.RevokeNone,
                UnionChoice = WinTrustNative.ChoiceCatalog,
                Union = catalogInfoPtr,
                StateAction = WinTrustNative.StateActionVerify,
                ProvFlags = WinTrustNative.ProvFlagsOffline,
            };
            var action = WinTrustNative.GenericVerifyV2;
            var result = WinTrustNative.WinVerifyTrust(-1, ref action, ref data);
            try
            {
                return result == 0 ? GetSignerSubject(data.StateData) : null;
            }
            finally
            {
                data.StateAction = WinTrustNative.StateActionClose;
                WinTrustNative.WinVerifyTrust(-1, ref action, ref data);
            }
        }
        finally
        {
            if (added)
            {
                file.DangerousRelease();
            }

            Marshal.FreeHGlobal(catalogInfoPtr);
            Marshal.FreeHGlobal(hashPtr);
            Marshal.FreeHGlobal(memberPathPtr);
            Marshal.FreeHGlobal(memberTagPtr);
            Marshal.FreeHGlobal(catalogPathPtr);
        }
    }

    private static string? GetSignerSubject(nint stateData)
    {
        var providerData = WinTrustNative.WTHelperProvDataFromStateData(stateData);
        if (providerData == 0)
        {
            return null;
        }

        var signer = WinTrustNative.WTHelperGetProvSignerFromChain(providerData, 0, false, 0);
        if (signer == 0)
        {
            return null;
        }

        var providerCert = WinTrustNative.WTHelperGetProvCertFromChain(signer, 0);
        if (providerCert == 0)
        {
            return null;
        }

        var head = Marshal.PtrToStructure<WinTrustNative.CryptProviderCertHead>(providerCert);
        if (head.Cert == 0)
        {
            return null;
        }

        using var certificate = new X509Certificate2(head.Cert);
        return certificate.Subject;
    }
}
