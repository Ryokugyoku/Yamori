namespace Yamori.Helper;

/// <summary>
/// Helper のサービスとしての名前など、登録に使う値。
/// </summary>
public static class HelperServiceInfo
{
    /// <summary>Windows サービスの名前（Package.appxmanifest と sc.exe での登録に使う名前と同じ）。</summary>
    public const string ServiceName = "YamoriHelper";
}
