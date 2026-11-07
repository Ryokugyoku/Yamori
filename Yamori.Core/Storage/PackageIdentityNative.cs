using System.Runtime.InteropServices;

namespace Yamori.Core.Storage;

/// <summary>
/// パッケージ（MSIX）の ID を調べる Win32 の関数。
/// </summary>
internal static partial class PackageIdentityNative
{
    /// <summary>パッケージの ID を持たないプロセスで返る値（APPMODEL_ERROR_NO_PACKAGE）。</summary>
    public const int AppModelErrorNoPackage = 15700;

    /// <summary>バッファーが足りないときに返る値（ERROR_INSUFFICIENT_BUFFER）。</summary>
    public const int ErrorInsufficientBuffer = 122;

    /// <summary>
    /// 今のプロセスのパッケージ ファミリ名を取得する。
    /// </summary>
    /// <param name="packageFamilyNameLength">バッファーの長さ（終端を含む文字数）。足りなければ必要な長さが入る。</param>
    /// <param name="packageFamilyName">名前を受け取るバッファー。</param>
    /// <returns>Win32 のエラーコード。成功したら 0。</returns>
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int GetCurrentPackageFamilyName(ref uint packageFamilyNameLength, [Out] char[]? packageFamilyName);
}
