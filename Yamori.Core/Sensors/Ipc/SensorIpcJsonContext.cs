using System.Text.Json.Serialization;
using Yamori.Core.Sensors.Ipc.Messages;

namespace Yamori.Core.Sensors.Ipc;

/// <summary>
/// パイプで送るメッセージの JSON の変換（ソース生成）。名前は camelCase、列挙値は数値、null と既定値の項目は書かない。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals)]
[JsonSerializable(typeof(IpcEnvelope))]
[JsonSerializable(typeof(SensorDescriptorDto[]))]
[JsonSerializable(typeof(SensorReadingDto[]))]
[JsonSerializable(typeof(SensorHistoryPointDto[]))]
[JsonSerializable(typeof(KeysRequest))]
[JsonSerializable(typeof(KeyRequest))]
[JsonSerializable(typeof(RecentRequest))]
[JsonSerializable(typeof(HistoryRequest))]
[JsonSerializable(typeof(SetLogPolicyRequest))]
[JsonSerializable(typeof(LogPolicyResponse))]
[JsonSerializable(typeof(SettingResultResponse))]
[JsonSerializable(typeof(ExtendedStateMessage))]
[JsonSerializable(typeof(ConsentMessage))]
[JsonSerializable(typeof(AppCpuUsageRequest))]
[JsonSerializable(typeof(AppCpuUsageResponse))]
[JsonSerializable(typeof(AnomaliesRequest))]
[JsonSerializable(typeof(AnomalyDto[]))]
[JsonSerializable(typeof(AnomalyNoticeDto))]
[JsonSerializable(typeof(AnomalyDismissRequest))]
[JsonSerializable(typeof(AnomalyIdRequest))]
[JsonSerializable(typeof(AnomalySettingDto))]
[JsonSerializable(typeof(AnomalySettingDto[]))]
[JsonSerializable(typeof(AnomalyNotificationsMessage))]
public sealed partial class SensorIpcJsonContext : JsonSerializerContext;
