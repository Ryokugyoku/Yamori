using System.Threading.Tasks;
using Yamori.Core.Sensors.Service;

namespace Yamori.Services;

/// <summary>
/// 管理者の承認（UAC）を得て、PawnIO の利用の同意を変える。GUI 自体は管理者として動かさず、承認した変更だけを管理者の権限で行う。
/// </summary>
public interface IElevatedConsentChanger
{
    /// <summary>
    /// 管理者の承認の画面を出し、承認されたら同意を変える。
    /// </summary>
    /// <param name="enabled">同意するなら true。</param>
    /// <returns>変更の結果。承認の画面で取り消された場合は null。</returns>
    Task<SensorSettingResult?> SetPawnIoConsentAsync(bool enabled);
}
