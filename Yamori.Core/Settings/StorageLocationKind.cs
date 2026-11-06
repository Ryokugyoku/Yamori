namespace Yamori.Core.Settings;

/// <summary>
/// 使用率の記録を保存する場所の種類。
/// </summary>
public enum StorageLocationKind
{
    /// <summary>この PC の中（<c>%LOCALAPPDATA%\Yamori</c>）。既定。</summary>
    Local = 0,

    /// <summary>OneDrive フォルダーの中。同期は OneDrive アプリが行い、Yamori 自身は通信しない。</summary>
    OneDrive = 1,

    /// <summary>ユーザーが指定したフォルダーの中。</summary>
    CustomFolder = 2,
}
