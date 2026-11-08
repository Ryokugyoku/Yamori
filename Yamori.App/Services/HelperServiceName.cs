namespace Yamori.Services;

/// <summary>
/// GUI がつなぐ Helper の Windows サービスの名前。
/// </summary>
public static class HelperServiceName
{
    /// <summary>
    /// 今のビルドで使うサービスの名前を取得する。
    /// Release は MSIX のパッケージ サービス（<c>YamoriHelper</c>）、Debug は <c>DevService.ps1</c> で登録する開発用のサービス（<c>YamoriHelperDev</c>）。
    /// </summary>
#if DEBUG
    public const string Current = "YamoriHelperDev";
#else
    public const string Current = "YamoriHelper";
#endif
}
