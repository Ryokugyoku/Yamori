namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// 1 種類のハードウェア部品の情報を読み取る。
/// </summary>
/// <typeparam name="T">読み取った結果の型。</typeparam>
public interface IHardwareReader<out T>
{
    /// <summary>
    /// 部品の情報を読み取る。
    /// </summary>
    /// <returns>読み取った情報。</returns>
    /// <exception cref="Wmi.WmiQueryException">情報源へのクエリに失敗した。</exception>
    T Read();
}
