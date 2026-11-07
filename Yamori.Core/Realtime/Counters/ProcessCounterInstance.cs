using System.Globalization;

namespace Yamori.Core.Realtime.Counters;

/// <summary>
/// プロセスを表すパフォーマンスカウンターのインスタンス名から、プロセス名と PID を読み解く。
/// </summary>
public static class ProcessCounterInstance
{
    private const string GpuProcessPrefix = "pid_";

    /// <summary>
    /// <c>\Process V2(*)</c> の <c>chrome:1234</c> のようなインスタンス名を、プロセス名と PID に分ける。
    /// </summary>
    /// <param name="instanceName">インスタンス名。</param>
    /// <returns>プロセス名と PID。形が違う場合は null。</returns>
    public static (string Name, int ProcessId)? TryParseNameAndId(string? instanceName)
    {
        if (string.IsNullOrEmpty(instanceName))
        {
            return null;
        }

        var colon = instanceName.LastIndexOf(':');
        if (colon <= 0
            || !int.TryParse(instanceName.AsSpan(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var processId))
        {
            return null;
        }

        return (instanceName[..colon], processId);
    }

    /// <summary>
    /// <c>\Process(*)</c> の <c>chrome#2</c> のようなインスタンス名から、同名のプロセスを見分けるための番号を除く。
    /// </summary>
    /// <param name="instanceName">インスタンス名。</param>
    /// <returns>プロセス名。番号が付いていなければそのまま。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="instanceName"/> が null。</exception>
    public static string StripDuplicateIndex(string instanceName)
    {
        ArgumentNullException.ThrowIfNull(instanceName);

        var hash = instanceName.LastIndexOf('#');
        return hash > 0
            && hash < instanceName.Length - 1
            && instanceName.AsSpan(hash + 1).IndexOfAnyExceptInRange('0', '9') < 0
            ? instanceName[..hash]
            : instanceName;
    }

    /// <summary>
    /// <c>\GPU Engine(*)</c> の <c>pid_1234_luid_..._engtype_3D</c> のようなインスタンス名から PID を取り出す。
    /// </summary>
    /// <param name="instanceName">インスタンス名。</param>
    /// <returns>PID。形が違う場合は null。</returns>
    public static int? TryParseGpuProcessId(string? instanceName)
    {
        if (instanceName is null || !instanceName.StartsWith(GpuProcessPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var rest = instanceName.AsSpan(GpuProcessPrefix.Length);
        var end = rest.IndexOf('_');
        return end > 0 && int.TryParse(rest[..end], NumberStyles.None, CultureInfo.InvariantCulture, out var processId)
            ? processId
            : null;
    }
}
