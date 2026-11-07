namespace Yamori.Core.Sensors.Providers;

/// <summary>
/// Provider の ID。設定ファイル（優先度）と記録（取得元）で使うため、決めた値は変えないこと。
/// </summary>
public static class SensorProviderIds
{
    /// <summary>Windows の標準 API（PDH、WMI、D3DKMT など）。管理者権限は不要。</summary>
    public const string WindowsStandard = "win-std";

    /// <summary>PawnIO（Helper 経由）。ユーザーが PawnIO を入れて同意した場合だけ使う。</summary>
    public const string PawnIo = "pawnio";
}
