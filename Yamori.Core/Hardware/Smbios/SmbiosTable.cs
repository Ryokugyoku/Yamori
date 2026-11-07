namespace Yamori.Core.Hardware.Smbios;

/// <summary>
/// 解析済みの SMBIOS テーブル。
/// </summary>
/// <param name="MajorVersion">SMBIOS のメジャーバージョン。</param>
/// <param name="MinorVersion">SMBIOS のマイナーバージョン。</param>
/// <param name="Structures">テーブルに含まれる構造体。</param>
public sealed record SmbiosTable(int MajorVersion, int MinorVersion, IReadOnlyList<SmbiosStructure> Structures)
{
    /// <summary>
    /// 指定した種類の構造体だけを取り出す。
    /// </summary>
    /// <param name="type">構造体の種類（例：<see cref="SmbiosStructureTypes.CoolingDevice"/>）。</param>
    /// <returns>該当する構造体。テーブル内の順番を保つ。</returns>
    public IEnumerable<SmbiosStructure> OfType(byte type) => Structures.Where(s => s.Type == type);
}
