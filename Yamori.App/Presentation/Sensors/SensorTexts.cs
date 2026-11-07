using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Extended;
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
}
