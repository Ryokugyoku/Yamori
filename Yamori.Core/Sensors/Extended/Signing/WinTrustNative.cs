using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Yamori.Core.Sensors.Extended.Signing;

/// <summary>
/// 署名の確認（WinVerifyTrust とカタログの検索）に使う Win32 の関数と構造体。
/// </summary>
internal static unsafe partial class WinTrustNative
{
    /// <summary>Authenticode の一般的な確認（WINTRUST_ACTION_GENERIC_VERIFY_V2）。</summary>
    public static readonly Guid GenericVerifyV2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    /// <summary>ドライバーのカタログのサブシステム（DRIVER_ACTION_VERIFY）。</summary>
    public static readonly Guid DriverActionVerify = new("F750E6C3-38EE-11d1-85E5-00C04FC295EE");

    /// <summary>UI を出さない（WTD_UI_NONE）。</summary>
    public const uint UiNone = 2;

    /// <summary>失効を確かめない（WTD_REVOKE_NONE）。</summary>
    public const uint RevokeNone = 0;

    /// <summary>カタログで確かめる（WTD_CHOICE_CATALOG）。</summary>
    public const uint ChoiceCatalog = 2;

    /// <summary>確かめて状態を残す（WTD_STATEACTION_VERIFY）。</summary>
    public const uint StateActionVerify = 1;

    /// <summary>残した状態を閉じる（WTD_STATEACTION_CLOSE）。</summary>
    public const uint StateActionClose = 2;

    /// <summary>ネットワークに出ない（WTD_REVOCATION_CHECK_NONE | WTD_CACHE_ONLY_URL_RETRIEVAL）。</summary>
    public const uint ProvFlagsOffline = 0x00000010 | 0x00001000;

    /// <summary>ファイルの署名を確かめる。</summary>
    [LibraryImport("wintrust.dll")]
    public static partial int WinVerifyTrust(nint hwnd, ref Guid action, ref WinTrustData data);

    /// <summary>カタログの管理の文脈を取得する。</summary>
    [LibraryImport("wintrust.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CryptCATAdminAcquireContext2(out nint catAdmin, ref Guid subsystem, string hashAlgorithm, nint strongHashPolicy, uint flags);

    /// <summary>カタログの管理の文脈を手放す。</summary>
    [LibraryImport("wintrust.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CryptCATAdminReleaseContext(nint catAdmin, uint flags);

    /// <summary>ファイルのハッシュを計算する。</summary>
    [LibraryImport("wintrust.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CryptCATAdminCalcHashFromFileHandle2(nint catAdmin, SafeFileHandle file, ref uint hashSize, byte[]? hash, uint flags);

    /// <summary>ハッシュを含むカタログを探す。</summary>
    [LibraryImport("wintrust.dll")]
    public static partial nint CryptCATAdminEnumCatalogFromHash(nint catAdmin, byte[] hash, uint hashSize, uint flags, ref nint previousCatalog);

    /// <summary>見つけたカタログを手放す。</summary>
    [LibraryImport("wintrust.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CryptCATAdminReleaseCatalogContext(nint catAdmin, nint catalog, uint flags);

    /// <summary>見つけたカタログのファイルのパスを取得する。</summary>
    [LibraryImport("wintrust.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CryptCATCatalogInfoFromContext(nint catalog, ref CatalogInfo info, uint flags);

    /// <summary>確かめた状態から、確認の結果のデータを取得する。</summary>
    [LibraryImport("wintrust.dll")]
    public static partial nint WTHelperProvDataFromStateData(nint stateData);

    /// <summary>確認の結果から、署名者を取得する。</summary>
    [LibraryImport("wintrust.dll")]
    public static partial nint WTHelperGetProvSignerFromChain(nint providerData, uint signerIndex, [MarshalAs(UnmanagedType.Bool)] bool counterSigner, uint counterSignerIndex);

    /// <summary>署名者から、証明書の連鎖の証明書を取得する。</summary>
    [LibraryImport("wintrust.dll")]
    public static partial nint WTHelperGetProvCertFromChain(nint signer, uint certIndex);

    /// <summary>WINTRUST_DATA。</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WinTrustData
    {
        public uint CbStruct;
        public nint PolicyCallbackData;
        public nint SipClientData;
        public uint UiChoice;
        public uint RevocationChecks;
        public uint UnionChoice;
        public nint Union;
        public uint StateAction;
        public nint StateData;
        public nint UrlReference;
        public uint ProvFlags;
        public uint UiContext;
        public nint SignatureSettings;
    }

    /// <summary>WINTRUST_CATALOG_INFO。</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WinTrustCatalogInfo
    {
        public uint CbStruct;
        public uint CatalogVersion;
        public nint CatalogFilePath;
        public nint MemberTag;
        public nint MemberFilePath;
        public nint MemberFile;
        public nint CalculatedFileHash;
        public uint CalculatedFileHashSize;
        public nint CatalogContext;
        public nint CatAdmin;
    }

    /// <summary>CATALOG_INFO。</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct CatalogInfo
    {
        public uint CbStruct;
        public fixed char CatalogFile[260];
    }

    /// <summary>CRYPT_PROVIDER_CERT の先頭（証明書のポインターまで）。</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CryptProviderCertHead
    {
        public uint CbStruct;
        public nint Cert;
    }
}
