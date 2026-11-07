using System.Buffers.Binary;

namespace Yamori.Core.Hardware.Topology;

/// <summary>
/// GetLogicalProcessorInformationEx(RelationProcessorCore) が返すバイト列から、コアの内訳を数える。
/// </summary>
/// <remarks>
/// 各コアの「効率クラス」（EfficiencyClass）を見て、最も大きいクラスを P コア、それ以外を E コアとする。
/// すべてのコアが同じクラスなら、ハイブリッド構成ではない。
/// </remarks>
public sealed class CoreTopologyParser
{
    /// <summary>RelationProcessorCore を表す値。</summary>
    private const uint RelationProcessorCore = 0;

    /// <summary>レコードの先頭から EfficiencyClass までの位置（Relationship・Size・Flags の後ろ）。</summary>
    private const int EfficiencyClassOffset = 9;

    /// <summary>
    /// バイト列を解析する。
    /// </summary>
    /// <param name="buffer">SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX の並び。</param>
    /// <returns>ハイブリッド構成の場合は内訳。そうでない場合や読めない場合は null。</returns>
    public CoreTopology? Parse(ReadOnlySpan<byte> buffer)
    {
        var coresByClass = new SortedDictionary<byte, int>();
        var position = 0;
        while (position + EfficiencyClassOffset < buffer.Length)
        {
            var relationship = BinaryPrimitives.ReadUInt32LittleEndian(buffer[position..]);
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(buffer[(position + 4)..]);
            if (size <= EfficiencyClassOffset || position + size > buffer.Length)
            {
                break;
            }

            if (relationship == RelationProcessorCore)
            {
                var efficiencyClass = buffer[position + EfficiencyClassOffset];
                coresByClass[efficiencyClass] = coresByClass.GetValueOrDefault(efficiencyClass) + 1;
            }

            position += size;
        }

        if (coresByClass.Count < 2)
        {
            return null;
        }

        var performance = coresByClass.Last().Value;
        var efficiency = coresByClass.Sum(p => p.Value) - performance;
        return new CoreTopology(performance, efficiency);
    }
}
