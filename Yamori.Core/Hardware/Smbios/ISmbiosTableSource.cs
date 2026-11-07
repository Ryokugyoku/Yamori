namespace Yamori.Core.Hardware.Smbios;

/// <summary>
/// ファームウェアの SMBIOS テーブルを提供する。テストで差し替えられるように抽象化している。
/// </summary>
public interface ISmbiosTableSource
{
    /// <summary>
    /// SMBIOS テーブルを読み取る。
    /// </summary>
    /// <returns>読み取ったテーブル。ファームウェアが提供していないか、読み取れない場合は null。</returns>
    SmbiosTable? Read();
}
