namespace Yamori.Core.Settings;

/// <summary>
/// ユーザーが変更できる Yamori の設定。
/// </summary>
/// <remarks>
/// センサーの記録は常駐サービス（Helper）が PC 全体で 1 か所に保存するため、保存先はここでは選ばない。
/// 以前の版の設定ファイルにある保存先の項目（<c>StorageLocation</c>・<c>CustomFolderPath</c>）は、読み込むときに無視する。
/// </remarks>
public sealed record YamoriSettings
{
    /// <summary>
    /// 既定の設定（Windows の言語に合わせる）を取得する。
    /// </summary>
    public static YamoriSettings Default { get; } = new();

    /// <summary>
    /// 画面に表示する言語を取得する。
    /// </summary>
    public AppLanguage Language { get; init; } = AppLanguage.System;
}
