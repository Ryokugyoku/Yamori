using System.Threading.Tasks;

namespace Yamori.Services;

/// <summary>
/// 止まっている Helper のサービスを起動する。権限が足りないときは、管理者の承認（UAC）を得て起動だけを管理者の権限で行う。
/// </summary>
public interface IHelperServiceStarter
{
    /// <summary>
    /// Helper のサービスを起動する。起動が終わるまでは待たない。
    /// </summary>
    /// <returns>起動の結果。</returns>
    Task<HelperStartResult> StartAsync();
}
