using Yamori.Core.Interop;

namespace Yamori.Core.Hardware.Topology;

/// <summary>
/// Windows の GetLogicalProcessorInformationEx API で、CPU のコアの内訳を読む。管理者権限は不要。
/// </summary>
public sealed class Win32CoreTopologySource : ICoreTopologySource
{
    /// <summary>RelationProcessorCore を表す値。</summary>
    private const int RelationProcessorCore = 0;

    private readonly CoreTopologyParser _parser;

    /// <summary>
    /// 解析処理を指定して作る。
    /// </summary>
    /// <param name="parser">バイト列の解析処理。省略時は新しく作る。</param>
    public Win32CoreTopologySource(CoreTopologyParser? parser = null)
    {
        _parser = parser ?? new CoreTopologyParser();
    }

    /// <inheritdoc />
    public CoreTopology? Read()
    {
        uint length = 0;
        NativeMethods.GetLogicalProcessorInformationEx(RelationProcessorCore, null, ref length);
        if (length == 0)
        {
            return null;
        }

        var buffer = new byte[length];
        if (!NativeMethods.GetLogicalProcessorInformationEx(RelationProcessorCore, buffer, ref length))
        {
            return null;
        }

        return _parser.Parse(buffer.AsSpan(0, (int)Math.Min(length, (uint)buffer.Length)));
    }
}
