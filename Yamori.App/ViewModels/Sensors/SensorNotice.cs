using Microsoft.UI.Xaml.Controls;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Ipc.Client;
using Yamori.Presentation.Sensors;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// リアルタイム画面の上に 1 か所だけ出すお知らせの内容。Helper とつながっていなければ接続の状態を、つながっていれば拡張センサーの状態を出す。
/// </summary>
/// <param name="Title">見出し。</param>
/// <param name="Message">本文。</param>
/// <param name="Severity">重さ（表示の色）。</param>
/// <param name="ConsentAction">PawnIO の利用の同意のボタンで、同意する（true）か取り消す（false）か。ボタンを出さないなら null。</param>
public sealed record SensorNotice(string Title, string Message, InfoBarSeverity Severity, bool? ConsentAction)
{
    /// <summary>まだ何も分かっていないときの内容を取得する。</summary>
    public static SensorNotice Empty { get; } = new(string.Empty, string.Empty, InfoBarSeverity.Informational, null);

    /// <summary>
    /// 接続と拡張センサーの状態から、お知らせの内容を決める。
    /// </summary>
    /// <param name="connection">Helper との接続の状態。</param>
    /// <param name="extended">拡張センサーの状態。</param>
    /// <returns>お知らせの内容。</returns>
    public static SensorNotice Describe(HelperConnectionState connection, ExtendedSensorState extended) => connection switch
    {
        HelperConnectionState.Connected => new(
            SensorTexts.ExtendedTitle,
            SensorTexts.ExtendedState(extended),
            extended switch
            {
                ExtendedSensorState.Active => InfoBarSeverity.Success,
                ExtendedSensorState.UnsupportedEdition or ExtendedSensorState.Unavailable => InfoBarSeverity.Warning,
                _ => InfoBarSeverity.Informational,
            },
            extended switch
            {
                ExtendedSensorState.ConsentRequired => true,
                ExtendedSensorState.Active or ExtendedSensorState.Unavailable => false,
                _ => null,
            }),
        _ => new(
            SensorTexts.HelperTitle,
            SensorTexts.HelperConnection(connection),
            connection == HelperConnectionState.Connecting ? InfoBarSeverity.Informational : InfoBarSeverity.Warning,
            null),
    };
}
