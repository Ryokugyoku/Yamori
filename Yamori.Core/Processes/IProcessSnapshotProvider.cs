namespace Yamori.Core.Processes;

/// <summary>
/// 実行中のプロセスの資源使用量を取得する。
/// </summary>
public interface IProcessSnapshotProvider
{
    /// <summary>
    /// 今、実行中のプロセスの一覧を取得する。
    /// </summary>
    /// <returns>プロセスごとの資源使用量。</returns>
    IReadOnlyList<ProcessSnapshot> Capture();
}
