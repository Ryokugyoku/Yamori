using Yamori.Core.Interop;

namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// Windows の GetLogicalProcessorInformationEx で、物理コアの並びと種類を読む。管理者権限は不要。
/// </summary>
public static class Win32CpuCoreLayoutSource
{
    private const int RelationProcessorCore = 0;

    /// <summary>
    /// 物理コアの並びを読む。
    /// </summary>
    /// <returns>物理コアの一覧。読めない場合は空。</returns>
    public static IReadOnlyList<CpuCore> Read()
    {
        uint length = 0;
        NativeMethods.GetLogicalProcessorInformationEx(RelationProcessorCore, null, ref length);
        if (length == 0)
        {
            return [];
        }

        var buffer = new byte[length];
        return NativeMethods.GetLogicalProcessorInformationEx(RelationProcessorCore, buffer, ref length)
            ? CpuCoreLayoutParser.Parse(buffer.AsSpan(0, (int)Math.Min(length, (uint)buffer.Length)))
            : [];
    }
}
