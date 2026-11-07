using System.Buffers.Binary;
using System.Text;

namespace Yamori.Core.Hardware.Smbios;

/// <summary>
/// GetSystemFirmwareTable('RSMB') が返すバイト列（RawSMBIOSData）を、構造体の一覧に分解する。
/// </summary>
/// <remarks>
/// 壊れたテーブルや途中で切れたテーブルでも例外にせず、読めたところまでを返す。
/// </remarks>
public sealed class SmbiosTableParser
{
    /// <summary>RawSMBIOSData の先頭の見出し（呼び出し方法・バージョン・長さ）の長さ。</summary>
    private const int RawHeaderLength = 8;

    /// <summary>構造体の見出し（種類・長さ・ハンドル）の長さ。</summary>
    private const int StructureHeaderLength = 4;

    /// <summary>
    /// バイト列を解析する。
    /// </summary>
    /// <param name="raw">RawSMBIOSData のバイト列。</param>
    /// <returns>解析したテーブル。見出しすら読めない場合は null。</returns>
    public SmbiosTable? Parse(ReadOnlySpan<byte> raw)
    {
        if (raw.Length < RawHeaderLength)
        {
            return null;
        }

        int major = raw[1];
        int minor = raw[2];
        var declaredLength = BinaryPrimitives.ReadUInt32LittleEndian(raw[4..]);
        var data = raw[RawHeaderLength..];
        if (declaredLength < (uint)data.Length)
        {
            data = data[..(int)declaredLength];
        }

        return new SmbiosTable(major, minor, ParseStructures(data));
    }

    private static List<SmbiosStructure> ParseStructures(ReadOnlySpan<byte> data)
    {
        var structures = new List<SmbiosStructure>();
        var position = 0;
        while (position + StructureHeaderLength <= data.Length)
        {
            var type = data[position];
            int length = data[position + 1];
            if (length < StructureHeaderLength || position + length > data.Length)
            {
                break;
            }

            var formatted = data.Slice(position, length).ToArray();
            var (strings, next) = ReadStrings(data, position + length);
            structures.Add(new SmbiosStructure(formatted, strings));

            if (type == SmbiosStructureTypes.EndOfTable || next < 0)
            {
                break;
            }

            position = next;
        }

        return structures;
    }

    /// <summary>
    /// 構造体の後ろに続く、0 で区切られた文字列の並び（最後は 0 が 2 つ）を読む。
    /// </summary>
    /// <returns>文字列の一覧と、次の構造体の位置。終わりが見つからない場合、位置は -1。</returns>
    private static (List<string> Strings, int Next) ReadStrings(ReadOnlySpan<byte> data, int start)
    {
        var strings = new List<string>();
        var position = start;

        // 文字列が 1 つもない構造体は、0 が 2 つだけ続く。
        if (position + 1 < data.Length && data[position] == 0 && data[position + 1] == 0)
        {
            return (strings, position + 2);
        }

        while (position < data.Length)
        {
            var end = data[position..].IndexOf((byte)0);
            if (end < 0)
            {
                return (strings, -1);
            }

            strings.Add(Encoding.ASCII.GetString(data.Slice(position, end)));
            position += end + 1;
            if (position < data.Length && data[position] == 0)
            {
                return (strings, position + 1);
            }
        }

        return (strings, -1);
    }
}
