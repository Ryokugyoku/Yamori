namespace Yamori.Core.Storage;

/// <summary>
/// OneDrive アプリが設定する環境変数から、OneDrive フォルダーを探す。OneDrive の API やネットワークは使わない。
/// </summary>
public sealed class OneDriveLocator : IOneDriveLocator
{
    /// <summary>
    /// 調べる環境変数。既定のアカウント、個人用、職場・学校用の順に探す。
    /// </summary>
    private static readonly string[] VariableNames = ["OneDrive", "OneDriveConsumer", "OneDriveCommercial"];

    private readonly Func<string, string?> _getEnvironmentVariable;
    private readonly Func<string, bool> _directoryExists;

    /// <summary>
    /// 実際の環境変数とファイルシステムを使って作る。
    /// </summary>
    public OneDriveLocator()
        : this(Environment.GetEnvironmentVariable, Directory.Exists)
    {
    }

    /// <summary>
    /// 環境変数の読み取りとフォルダーの存在確認の処理を指定して作る（テスト用）。
    /// </summary>
    /// <param name="getEnvironmentVariable">環境変数を読む処理。</param>
    /// <param name="directoryExists">フォルダーがあるかを調べる処理。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public OneDriveLocator(Func<string, string?> getEnvironmentVariable, Func<string, bool> directoryExists)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        ArgumentNullException.ThrowIfNull(directoryExists);
        _getEnvironmentVariable = getEnvironmentVariable;
        _directoryExists = directoryExists;
    }

    /// <inheritdoc />
    public string? FindRoot()
    {
        foreach (var name in VariableNames)
        {
            var path = _getEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path) && _directoryExists(path))
            {
                return path;
            }
        }

        return null;
    }
}
