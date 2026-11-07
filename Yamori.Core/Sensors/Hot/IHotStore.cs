namespace Yamori.Core.Sensors.Hot;

/// <summary>
/// 最新値と直近の履歴を、プロセスのメモリの中だけに持つストア（Hot 層）。サマリー表示とリアルタイムのグラフに使う。永続化はしない。
/// </summary>
/// <remarks>実装はスレッドセーフであること（書き込みは収集のスレッド、読み取りは UI や IPC のスレッドから行う）。</remarks>
public interface IHotStore
{
    /// <summary>
    /// <see cref="Append"/> で新しい値を受け取ったときに発生する。引数は受け取った値。書き込んだスレッドで発生する。
    /// </summary>
    event EventHandler<IReadOnlyList<SensorReading>>? Appended;

    /// <summary>
    /// 新しい値を書き込む。センサーごとの履歴が上限に達したら、古いものから捨てる。
    /// </summary>
    /// <param name="readings">書き込む値。</param>
    /// <exception cref="ArgumentNullException"><paramref name="readings"/> が null。</exception>
    void Append(IReadOnlyList<SensorReading> readings);

    /// <summary>
    /// センサーの最新値を取得する。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <returns>最新値。まだ 1 つも書き込まれていなければ null。</returns>
    SensorReading? GetLatest(SensorKey key);

    /// <summary>
    /// 直近の区間の履歴を、古い順に取得する。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <param name="window">今からさかのぼる長さ。</param>
    /// <returns>区間内の値。なければ空。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="window"/> が負の値。</exception>
    IReadOnlyList<SensorReading> GetRecent(SensorKey key, TimeSpan window);

    /// <summary>
    /// 指定したキー以外の履歴を捨てる。カタログから消えたセンサー（取り外した GPU など）の履歴を残さないために使う。
    /// </summary>
    /// <param name="keys">残すキー。</param>
    /// <exception cref="ArgumentNullException"><paramref name="keys"/> が null。</exception>
    void RetainOnly(IReadOnlySet<SensorKey> keys);
}
