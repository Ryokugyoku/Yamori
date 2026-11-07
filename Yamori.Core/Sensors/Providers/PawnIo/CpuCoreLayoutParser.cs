using System.Buffers.Binary;
using System.Numerics;

namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// GetLogicalProcessorInformationEx(RelationProcessorCore) が返すバイト列から、物理コアの並びと種類（P コア・E コア）を読む。
/// </summary>
/// <remarks>
/// 効率クラス（EfficiencyClass）が最も大きいコアを P コア、それより小さいコアを E コアとする。すべて同じなら <see cref="CpuCoreKind.Uniform"/>。
/// </remarks>
public static class CpuCoreLayoutParser
{
    private const uint RelationProcessorCore = 0;
    private const int EfficiencyClassOffset = 9;
    private const int GroupCountOffset = 30;
    private const int FirstGroupMaskOffset = 32;
    private const int GroupAffinitySize = 16;

    /// <summary>
    /// バイト列を読む。
    /// </summary>
    /// <param name="buffer">SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX の並び。</param>
    /// <returns>物理コアの一覧（グループ、論理プロセッサの順）。読めない場合は空。</returns>
    public static IReadOnlyList<CpuCore> Parse(ReadOnlySpan<byte> buffer)
    {
        var raw = new List<(ushort Group, byte Processor, byte EfficiencyClass)>();
        var position = 0;
        while (position + 8 <= buffer.Length)
        {
            var relationship = BinaryPrimitives.ReadUInt32LittleEndian(buffer[position..]);
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(buffer[(position + 4)..]);
            if (size < FirstGroupMaskOffset + GroupAffinitySize || position + size > buffer.Length)
            {
                break;
            }

            var record = buffer.Slice(position, size);
            if (relationship == RelationProcessorCore
                && BinaryPrimitives.ReadUInt16LittleEndian(record[GroupCountOffset..]) > 0)
            {
                var mask = BinaryPrimitives.ReadUInt64LittleEndian(record[FirstGroupMaskOffset..]);
                var group = BinaryPrimitives.ReadUInt16LittleEndian(record[(FirstGroupMaskOffset + 8)..]);
                if (mask != 0)
                {
                    raw.Add((group, (byte)BitOperations.TrailingZeroCount(mask), record[EfficiencyClassOffset]));
                }
            }

            position += size;
        }

        if (raw.Count == 0)
        {
            return [];
        }

        var maxClass = raw.Max(c => c.EfficiencyClass);
        var hybrid = raw.Any(c => c.EfficiencyClass != maxClass);
        return raw
            .OrderBy(c => c.Group)
            .ThenBy(c => c.Processor)
            .Select((c, index) => new CpuCore(
                index,
                c.Group,
                c.Processor,
                !hybrid ? CpuCoreKind.Uniform : c.EfficiencyClass == maxClass ? CpuCoreKind.Performance : CpuCoreKind.Efficiency))
            .ToArray();
    }
}
