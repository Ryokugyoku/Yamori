namespace Yamori.Core.Settings;

/// <summary>
/// 設定を読み書きする。
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// 保存されている設定を読み込む。
    /// </summary>
    /// <returns>保存されている設定。まだ保存されていないか、読めないときは <see cref="YamoriSettings.Default"/>。</returns>
    YamoriSettings Load();

    /// <summary>
    /// 設定を保存する。
    /// </summary>
    /// <param name="settings">保存する設定。</param>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> が null。</exception>
    /// <exception cref="IOException">書き込みに失敗した。</exception>
    void Save(YamoriSettings settings);
}
