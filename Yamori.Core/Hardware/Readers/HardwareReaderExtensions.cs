using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// <see cref="IHardwareReader{T}"/> の便利な使い方をまとめる。
/// </summary>
public static class HardwareReaderExtensions
{
    /// <summary>
    /// 部品の情報を読み取り、情報源へのクエリに失敗した場合は代わりの値を返す。
    /// 1 つの部品が読めなくても、他の部品の表示を続けるために使う。
    /// </summary>
    /// <typeparam name="T">読み取った結果の型。</typeparam>
    /// <param name="reader">読み取り処理。</param>
    /// <param name="fallback">失敗したときに返す値。</param>
    /// <returns>読み取った情報、または <paramref name="fallback"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> が null。</exception>
    public static T ReadOrDefault<T>(this IHardwareReader<T> reader, T fallback)
    {
        ArgumentNullException.ThrowIfNull(reader);
        try
        {
            return reader.Read();
        }
        catch (WmiQueryException)
        {
            return fallback;
        }
    }
}
