using System.Text;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// テスト用に、GetSystemFirmwareTable('RSMB') と同じ形のバイト列を組み立てる。
/// </summary>
internal static class SmbiosBytes
{
    /// <summary>
    /// 構造体を並べた RawSMBIOSData を作る。最後に End-of-Table を付ける。
    /// </summary>
    /// <param name="structures">各構造体の、決まった位置の値（種類・長さ・ハンドルを含む）と文字列。</param>
    /// <returns>RawSMBIOSData のバイト列（SMBIOS 3.4）。</returns>
    public static byte[] Build(params (byte[] Formatted, string[] Strings)[] structures)
    {
        var table = new List<byte>();
        foreach (var (formatted, strings) in structures.Append(([127, 4, 0xFF, 0xFF], [])))
        {
            table.AddRange(formatted);
            if (strings.Length == 0)
            {
                table.AddRange([0, 0]);
                continue;
            }

            foreach (var text in strings)
            {
                table.AddRange(Encoding.ASCII.GetBytes(text));
                table.Add(0);
            }

            table.Add(0);
        }

        var header = new byte[] { 0, 3, 4, 0 }.Concat(BitConverter.GetBytes((uint)table.Count));
        return header.Concat(table).ToArray();
    }
}
