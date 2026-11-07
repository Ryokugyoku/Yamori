using Yamori.Core.Sensors.Providers;

namespace Yamori.Core.Sensors.Registry;

/// <summary>
/// 同じキーを複数の Provider が提供するときに、どれを採用するかの設定。設定ファイル（JSON）から読む。
/// </summary>
/// <param name="DefaultOrder">規則に当てはまらないキーでの、Provider の優先順。</param>
/// <param name="Rules">キーのパターンごとの規則。上にあるものほど優先し、最初に一致した規則を使う。</param>
public sealed record SensorPriorityOptions(IReadOnlyList<string> DefaultOrder, IReadOnlyList<SensorPriorityRule> Rules)
{
    /// <summary>
    /// 設定ファイルがないときの既定の設定を取得する。低レベルなセンサーを直接読める PawnIO を、標準 API より優先する。
    /// </summary>
    public static SensorPriorityOptions Default { get; } =
        new([SensorProviderIds.PawnIo, SensorProviderIds.WindowsStandard], []);
}
