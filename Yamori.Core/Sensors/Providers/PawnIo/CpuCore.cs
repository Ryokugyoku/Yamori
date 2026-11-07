using System.Globalization;

namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// 物理コア 1 つ。コアごとの MSR は、このコアの最初の論理プロセッサにスレッドを固定して読む。
/// </summary>
/// <param name="Index">コアの番号（0 から。プロセッサ グループ、論理プロセッサの順）。</param>
/// <param name="Group">プロセッサ グループ。</param>
/// <param name="LogicalProcessor">グループの中の、このコアの最初の論理プロセッサの番号。</param>
/// <param name="Kind">コアの種類。</param>
public sealed record CpuCore(int Index, ushort Group, byte LogicalProcessor, CpuCoreKind Kind)
{
    /// <summary>
    /// 表示名を取得する（例：<c>P-Core #0</c>、<c>E-Core #8</c>、ハイブリッドでなければ <c>Core #0</c>）。
    /// </summary>
    public string DisplayName => Kind switch
    {
        CpuCoreKind.Performance => "P-Core #",
        CpuCoreKind.Efficiency => "E-Core #",
        _ => "Core #",
    } + Index.ToString(CultureInfo.InvariantCulture);

    /// <summary>キーに使う名前（例：<c>core-0</c>）を取得する。</summary>
    public string KeyName => "core-" + Index.ToString(CultureInfo.InvariantCulture);
}
