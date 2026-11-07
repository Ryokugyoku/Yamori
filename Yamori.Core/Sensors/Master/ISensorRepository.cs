using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Sensors.Master;

/// <summary>
/// マスタの <c>sensor</c> を読み書きする。センサーの番号（<c>sensor_id</c>）はここで採番する。
/// </summary>
public interface ISensorRepository
{
    /// <summary>
    /// センサーがなければ追加し、あれば単位と名前を更新する。記録の設定は、追加するときだけ <paramref name="defaultPolicy"/> を使い、既存の設定は変えない。
    /// </summary>
    /// <param name="descriptor">センサーの説明。</param>
    /// <param name="hardwareId">所属するハードウェアの行の番号。</param>
    /// <param name="defaultPolicy">新しく追加するときの記録の設定。</param>
    /// <returns>追加・更新した行。</returns>
    SensorRecord Ensure(SensorDescriptor descriptor, long hardwareId, SensorLogPolicy defaultPolicy);

    /// <summary>
    /// キーでセンサーを探す。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <returns>見つかった行。なければ null。</returns>
    SensorRecord? Find(SensorKey key);

    /// <summary>
    /// すべてのセンサーを、番号の順に取得する（今は見つからないものも含む）。
    /// </summary>
    /// <returns>すべての行。</returns>
    IReadOnlyList<SensorRecord> GetAll();

    /// <summary>
    /// 記録の設定を変える。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <param name="policy">新しい設定。</param>
    /// <returns>変えた行。センサーがなければ null。</returns>
    SensorRecord? UpdateLogPolicy(SensorKey key, SensorLogPolicy policy);
}
