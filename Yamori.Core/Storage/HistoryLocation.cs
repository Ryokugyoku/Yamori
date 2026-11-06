using Yamori.Core.Settings;

namespace Yamori.Core.Storage;

/// <summary>
/// 設定から決まった、記録の実際の保存先。
/// </summary>
/// <param name="Directory">履歴の CSV を置くフォルダーの絶対パス。</param>
/// <param name="Kind">実際に使っている保存先の種類。</param>
/// <param name="IsFallback">選んだ保存先が使えず、この PC の中に保存しているときは true。</param>
public sealed record HistoryLocation(string Directory, StorageLocationKind Kind, bool IsFallback)
{
    /// <summary>
    /// 保存した記録が、OneDrive アプリなどによって PC の外へ同期されうるかどうかを取得する。
    /// </summary>
    public bool MayLeaveThisPc => Kind == StorageLocationKind.OneDrive;
}
