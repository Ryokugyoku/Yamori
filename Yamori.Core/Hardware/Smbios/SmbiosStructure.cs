using System.Buffers.Binary;

namespace Yamori.Core.Hardware.Smbios;

/// <summary>
/// SMBIOS の構造体 1 つ。決まった位置の値（formatted area）と、後ろに続く文字列の一覧を持つ。
/// </summary>
public sealed class SmbiosStructure
{
    private readonly byte[] _formatted;
    private readonly IReadOnlyList<string> _strings;

    /// <summary>
    /// 構造体の中身を指定して作る。
    /// </summary>
    /// <param name="formatted">構造体の先頭（種類・長さ・ハンドルを含む）から、長さの分のバイト列。</param>
    /// <param name="strings">構造体の後ろに続く文字列。1 番目の文字列が添字 0 に入る。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    /// <exception cref="ArgumentException"><paramref name="formatted"/> が 4 バイトより短い。</exception>
    public SmbiosStructure(byte[] formatted, IReadOnlyList<string> strings)
    {
        ArgumentNullException.ThrowIfNull(formatted);
        ArgumentNullException.ThrowIfNull(strings);
        if (formatted.Length < 4)
        {
            throw new ArgumentException("SMBIOS の構造体は 4 バイト以上必要です。", nameof(formatted));
        }

        _formatted = formatted;
        _strings = strings;
    }

    /// <summary>構造体の種類の番号。</summary>
    public byte Type => _formatted[0];

    /// <summary>決まった位置の値の部分の長さ（バイト）。</summary>
    public int Length => _formatted.Length;

    /// <summary>構造体のハンドル（テーブル内の識別番号）。</summary>
    public ushort Handle => BinaryPrimitives.ReadUInt16LittleEndian(_formatted.AsSpan(2));

    /// <summary>
    /// 指定した位置の 1 バイトを取り出す。
    /// </summary>
    /// <param name="offset">構造体の先頭からの位置。</param>
    /// <returns>構造体がその位置まで届かない場合は null。</returns>
    public byte? GetByte(int offset) =>
        offset >= 0 && offset < _formatted.Length ? _formatted[offset] : null;

    /// <summary>
    /// 指定した位置の 2 バイト（リトルエンディアン）を取り出す。
    /// </summary>
    /// <param name="offset">構造体の先頭からの位置。</param>
    /// <returns>構造体がその位置まで届かない場合は null。</returns>
    public ushort? GetWord(int offset) =>
        offset >= 0 && offset + 2 <= _formatted.Length
            ? BinaryPrimitives.ReadUInt16LittleEndian(_formatted.AsSpan(offset))
            : null;

    /// <summary>
    /// 指定した位置にある文字列番号が指す文字列を取り出す。
    /// </summary>
    /// <param name="offset">文字列番号が入っている、構造体の先頭からの位置。</param>
    /// <returns>文字列。番号が 0（文字列なし）か範囲外の場合は null。</returns>
    public string? GetString(int offset) =>
        GetByte(offset) is { } index && index > 0 && index <= _strings.Count ? _strings[index - 1] : null;
}
