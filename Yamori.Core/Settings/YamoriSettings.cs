namespace Yamori.Core.Settings;

/// <summary>
/// ユーザーが変更できる Yamori の設定。
/// </summary>
public sealed record YamoriSettings
{
    /// <summary>
    /// 既定の設定（この PC に保存、Windows の言語に合わせる）を取得する。
    /// </summary>
    public static YamoriSettings Default { get; } = new();

    /// <summary>
    /// 記録の保存先の種類を取得する。
    /// </summary>
    public StorageLocationKind StorageLocation { get; init; } = StorageLocationKind.Local;

    /// <summary>
    /// <see cref="StorageLocationKind.CustomFolder"/> のときの保存先フォルダーを取得する。未指定なら null。
    /// </summary>
    public string? CustomFolderPath { get; init; }

    /// <summary>
    /// 画面に表示する言語を取得する。
    /// </summary>
    public AppLanguage Language { get; init; } = AppLanguage.System;
}
