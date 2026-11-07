using System.Globalization;

namespace Yamori.Core.Hardware.Wmi;

/// <summary>
/// WMI クエリ結果の 1 行。プロパティ名をキーにした値を、型を指定して取り出せるようにする。
/// </summary>
public sealed class WmiRecord
{
    private readonly IReadOnlyDictionary<string, object?> _properties;

    /// <summary>
    /// プロパティの辞書から <see cref="WmiRecord"/> を作る。
    /// </summary>
    /// <param name="properties">プロパティ名と値の組。名前の大文字・小文字は区別しない。</param>
    /// <exception cref="ArgumentNullException"><paramref name="properties"/> が null。</exception>
    public WmiRecord(IReadOnlyDictionary<string, object?> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        _properties = new Dictionary<string, object?>(properties, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 文字列としてプロパティを取り出す。
    /// </summary>
    /// <param name="name">プロパティ名。</param>
    /// <returns>前後の空白を除いた値。存在しないか空の場合は null。</returns>
    public string? GetString(string name)
    {
        var text = GetRaw(name)?.ToString()?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>
    /// 符号なし 64 ビット整数としてプロパティを取り出す。
    /// </summary>
    /// <param name="name">プロパティ名。</param>
    /// <returns>変換できない場合は null。</returns>
    public ulong? GetUInt64(string name) =>
        GetRaw(name) switch
        {
            null => null,
            ulong u => u,
            IConvertible c when TryConvert(c, out var value) => value,
            _ => null,
        };

    /// <summary>
    /// 32 ビット整数としてプロパティを取り出す。
    /// </summary>
    /// <param name="name">プロパティ名。</param>
    /// <returns>変換できないか、範囲外の場合は null。</returns>
    public int? GetInt32(string name) =>
        GetUInt64(name) is { } value && value <= int.MaxValue ? (int)value : null;

    /// <summary>
    /// 真偽値としてプロパティを取り出す。
    /// </summary>
    /// <param name="name">プロパティ名。</param>
    /// <returns>変換できない場合は null。</returns>
    public bool? GetBoolean(string name) =>
        GetRaw(name) switch
        {
            bool flag => flag,
            string text when bool.TryParse(text, out var parsed) => parsed,
            _ => null,
        };

    /// <summary>
    /// 日付としてプロパティを取り出す。CIM の DATETIME 形式（例：<c>20241004000000.000000+000</c>）にも対応する。
    /// </summary>
    /// <param name="name">プロパティ名。</param>
    /// <returns>変換できない場合は null。</returns>
    public DateOnly? GetDate(string name) =>
        GetRaw(name) switch
        {
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            DateTimeOffset offset => DateOnly.FromDateTime(offset.DateTime),
            string text when text.Length >= 8
                && DateOnly.TryParseExact(text[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) => date,
            _ => null,
        };

    /// <summary>
    /// 32 ビット整数の配列としてプロパティを取り出す。変換できない要素は除く。
    /// </summary>
    /// <param name="name">プロパティ名。</param>
    /// <returns>存在しないか配列でない場合は空。</returns>
    public IReadOnlyList<int> GetInt32Array(string name)
    {
        if (GetRaw(name) is not Array array)
        {
            return [];
        }

        var values = new List<int>(array.Length);
        foreach (var item in array)
        {
            if (item is IConvertible c && TryConvert(c, out var value) && value <= int.MaxValue)
            {
                values.Add((int)value);
            }
        }

        return values;
    }

    /// <summary>
    /// 変換前の値を取り出す。
    /// </summary>
    /// <param name="name">プロパティ名。</param>
    /// <returns>存在しない場合は null。</returns>
    public object? GetRaw(string name) =>
        _properties.TryGetValue(name, out var value) ? value : null;

    private static bool TryConvert(IConvertible convertible, out ulong value)
    {
        try
        {
            var number = Convert.ToDecimal(convertible, CultureInfo.InvariantCulture);
            if (number >= 0 && number <= ulong.MaxValue)
            {
                value = (ulong)number;
                return true;
            }
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            // 数値として読めない値は「取得できない」として扱う。
        }

        value = 0;
        return false;
    }
}
