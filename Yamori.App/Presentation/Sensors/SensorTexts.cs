using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Ipc.Client;
using Yamori.Core.Sensors.Service;
using Yamori.Localization;

namespace Yamori.Presentation.Sensors;

/// <summary>
/// センサーの表示で使う、UI の言語に合わせた文字列。
/// </summary>
public static class SensorTexts
{
    /// <summary>
    /// サマリーカードの見出しを取得する。
    /// </summary>
    /// <param name="cardId">カードの ID。</param>
    /// <returns>見出し。</returns>
    public static string CardTitle(string cardId) => UiText.Get("SummaryCard_" + cardId);

    /// <summary>
    /// ハードウェアの種類の名前を取得する。
    /// </summary>
    /// <param name="kind">ハードウェアの種類。</param>
    /// <returns>名前。</returns>
    public static string KindName(HardwareKind kind) => UiText.Get("SensorKind_" + kind);

    /// <summary>
    /// グループの名前を取得する。リソースにないグループは、グループの名前をそのまま返す。
    /// </summary>
    /// <param name="group">グループ（<see cref="SensorDescriptor.Group"/>）。</param>
    /// <returns>名前。</returns>
    public static string GroupName(string group)
    {
        var text = UiText.Get("SensorGroup_" + group);
        return text.StartsWith('[') ? group : text;
    }

    /// <summary>
    /// 拡張センサーの状態の説明を取得する。
    /// </summary>
    /// <param name="state">拡張センサーの状態。</param>
    /// <returns>説明。</returns>
    public static string ExtendedState(ExtendedSensorState state) => UiText.Get("Extended_" + state);

    /// <summary>拡張センサーのお知らせの見出しを取得する。</summary>
    public static string ExtendedTitle => UiText.Get("Extended_Title");

    /// <summary>Helper との接続のお知らせの見出しを取得する。</summary>
    public static string HelperTitle => UiText.Get("Helper_Title");

    /// <summary>
    /// Helper との接続の状態の説明を取得する。
    /// </summary>
    /// <param name="state">接続の状態。</param>
    /// <returns>説明。</returns>
    public static string HelperConnection(HelperConnectionState state) => UiText.Get("Helper_" + state);

    /// <summary>
    /// PawnIO の利用の同意のボタンの文字列を取得する。
    /// </summary>
    /// <param name="enable">同意するボタンなら true、取り消すボタンなら false。</param>
    /// <returns>ボタンの文字列。</returns>
    public static string ConsentAction(bool enable) => UiText.Get(enable ? "Consent_Enable" : "Consent_Disable");

    /// <summary>
    /// 同意を変えられなかった理由の説明を取得する。
    /// </summary>
    /// <param name="result">変更の結果。</param>
    /// <returns>説明。</returns>
    public static string ConsentResult(SensorSettingResult result) => UiText.Get("Consent_Result_" + result);
}
