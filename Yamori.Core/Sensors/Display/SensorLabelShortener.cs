namespace Yamori.Core.Sensors.Display;

/// <summary>
/// 同じカードに並ぶセンサーの表示名を短くする（小さなマスでも名前が切れないように）。
/// </summary>
public static class SensorLabelShortener
{
    /// <summary>
    /// すべての名前に共通する末尾を、語の切れ目（空白）から取り除く。
    /// 例：<c>Thread #0 Clock (estimated)</c>、<c>Thread #1 Clock (estimated)</c> → <c>Thread #0</c>、<c>Thread #1</c>。
    /// </summary>
    /// <param name="labels">名前。</param>
    /// <returns>
    /// 短くした名前（同じ順）と、取り除いた末尾（前後の空白は除く）。名前が 2 つ未満のとき、共通の末尾がないとき、
    /// 取り除くと空になる名前があるときは、元の名前と空の末尾を返す。
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="labels"/> が null。</exception>
    public static (IReadOnlyList<string> Labels, string Suffix) StripCommonSuffix(IReadOnlyList<string> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);
        if (labels.Count < 2)
        {
            return (labels, string.Empty);
        }

        var length = CommonSuffixLength(labels);

        // 語の途中から始まる末尾は使わない。末尾の中で最初の空白から先だけを、共通の末尾とみなす。
        var first = labels[0];
        var space = first.IndexOf(' ', first.Length - length);
        if (length == 0 || space < 0)
        {
            return (labels, string.Empty);
        }

        var suffixLength = first.Length - space;
        if (labels.Any(l => l.Length - suffixLength <= 0 || string.IsNullOrWhiteSpace(l[..^suffixLength])))
        {
            return (labels, string.Empty);
        }

        return (labels.Select(l => l[..^suffixLength].TrimEnd()).ToArray(), first[^suffixLength..].Trim());
    }

    private static int CommonSuffixLength(IReadOnlyList<string> labels)
    {
        var first = labels[0];
        var length = first.Length;
        foreach (var label in labels.Skip(1))
        {
            var max = Math.Min(length, label.Length);
            var common = 0;
            while (common < max && first[^(common + 1)] == label[^(common + 1)])
            {
                common++;
            }

            length = common;
        }

        return length;
    }
}
