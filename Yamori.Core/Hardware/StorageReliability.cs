namespace Yamori.Core.Hardware;

/// <summary>
/// ストレージの信頼性の情報（MSFT_StorageReliabilityCounter）。Windows はこれを管理者として実行したときだけ公開することが多い。
/// 温度だけは、取れない場合にディスクへの直接の問い合わせ（IOCTL）で補う。
/// </summary>
/// <param name="TemperatureCelsius">現在の温度（℃）。取得できない場合は null。</param>
/// <param name="WearPercent">消耗度（%。0 が新品、100 で寿命の目安）。取得できない場合は null。</param>
/// <param name="PowerOnHours">通電時間の累計（時間）。取得できない場合は null。</param>
public sealed record StorageReliability(int? TemperatureCelsius, int? WearPercent, int? PowerOnHours);
