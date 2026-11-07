using System.Globalization;
using System.Text.RegularExpressions;

namespace Yamori.Core.Realtime.Counters;

/// <summary>
/// GPU のパフォーマンスカウンターのインスタンス名を読み解いた結果。
/// </summary>
/// <param name="Luid">GPU アダプターの LUID（上位 32 ビットと下位 32 ビットをつなげた値）。</param>
/// <param name="EngineIndex">エンジンの番号。エンジンを含まないインスタンス（メモリのカウンター）では null。</param>
/// <param name="EngineType">エンジンの種類（<c>3D</c>、<c>Copy</c>、<c>VideoDecode</c> など）。エンジンを含まないインスタンスでは null。</param>
public sealed partial record GpuCounterInstance(ulong Luid, int? EngineIndex, string? EngineType)
{
    /// <summary>
    /// インスタンス名を読み解く。
    /// </summary>
    /// <param name="instanceName">例：<c>pid_1234_luid_0x00000000_0x0000F69A_phys_0_eng_3_engtype_VideoDecode</c>、<c>luid_0x00000000_0x0000f69a_phys_0</c>。</param>
    /// <returns>読み解いた結果。形が違う場合は null。</returns>
    public static GpuCounterInstance? TryParse(string? instanceName)
    {
        if (string.IsNullOrEmpty(instanceName))
        {
            return null;
        }

        var match = Pattern().Match(instanceName);
        if (!match.Success
            || !uint.TryParse(match.Groups["high"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var high)
            || !uint.TryParse(match.Groups["low"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var low))
        {
            return null;
        }

        int? engine = int.TryParse(match.Groups["eng"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var index) ? index : null;
        var type = match.Groups["type"].Success ? match.Groups["type"].Value : null;
        return new GpuCounterInstance(ToLuid(high, low), engine, type);
    }

    /// <summary>
    /// LUID の上位・下位の 32 ビットを 1 つの値にする。
    /// </summary>
    /// <param name="highPart">上位 32 ビット。</param>
    /// <param name="lowPart">下位 32 ビット。</param>
    /// <returns>つなげた値。</returns>
    public static ulong ToLuid(uint highPart, uint lowPart) => ((ulong)highPart << 32) | lowPart;

    [GeneratedRegex(@"luid_0x(?<high>[0-9a-fA-F]{1,8})_0x(?<low>[0-9a-fA-F]{1,8})_phys_\d+(?:_eng_(?<eng>\d+)_engtype_(?<type>.+))?$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
