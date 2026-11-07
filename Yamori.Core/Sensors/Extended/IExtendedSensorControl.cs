using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Sensors.Extended;

/// <summary>
/// 拡張センサー（PawnIO）を使うかどうかの判断と、利用の同意の扱い。
/// </summary>
public interface IExtendedSensorControl
{
    /// <summary>
    /// 同意が変わったときに発生する。センサーの宣言をやり直すきっかけに使う。任意のスレッドから発生する。
    /// </summary>
    event EventHandler? ConsentChanged;

    /// <summary>ユーザーが PawnIO の利用に同意しているかどうかを取得する。</summary>
    bool IsConsented { get; }

    /// <summary>
    /// PawnIO の Provider に読み取りを許すかどうかを取得する（入っている、公式版、同意済みのすべてを満たすとき true）。
    /// </summary>
    bool CanUsePawnIo { get; }

    /// <summary>
    /// 今の状態を決める。
    /// </summary>
    /// <param name="discoveryFailed">PawnIO の Provider の最後の宣言が失敗したかどうか。</param>
    /// <returns>状態。</returns>
    ExtendedSensorState GetState(bool discoveryFailed);

    /// <summary>
    /// 同意を変えて保存する。
    /// </summary>
    /// <param name="enabled">同意するなら true。</param>
    /// <returns>結果。</returns>
    SensorSettingResult SetConsent(bool enabled);
}
