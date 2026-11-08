using System.Text.Json.Serialization;

namespace Yamori.Core.Anomalies.Storage;

/// <summary>
/// マスタに置く異常の JSON（判断の理由の数値）の変換（ソース生成）。
/// </summary>
[JsonSerializable(typeof(Dictionary<string, double>))]
internal sealed partial class AnomalyStorageJsonContext : JsonSerializerContext;
