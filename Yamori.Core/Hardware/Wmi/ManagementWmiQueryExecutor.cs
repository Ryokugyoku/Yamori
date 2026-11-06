using System.Management;
using System.Runtime.InteropServices;

namespace Yamori.Core.Hardware.Wmi;

/// <summary>
/// System.Management を使って、ローカル PC の WMI にクエリを投げる。ネットワーク越しの接続はしない。
/// </summary>
public sealed class ManagementWmiQueryExecutor : IWmiQueryExecutor
{
    /// <inheritdoc />
    public IReadOnlyList<WmiRecord> Query(string wmiNamespace, string query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wmiNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        try
        {
            var scope = new ManagementScope(wmiNamespace);
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
            using var results = searcher.Get();

            var records = new List<WmiRecord>();
            foreach (var item in results)
            {
                using (item)
                {
                    records.Add(ToRecord(item));
                }
            }

            return records;
        }
        catch (Exception ex) when (ex is ManagementException or COMException or UnauthorizedAccessException)
        {
            throw new WmiQueryException($"WMI クエリに失敗しました（{wmiNamespace}: {query}）。", ex);
        }
    }

    private static WmiRecord ToRecord(ManagementBaseObject item)
    {
        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in item.Properties)
        {
            properties[property.Name] = property.Value;
        }

        return new WmiRecord(properties);
    }
}
