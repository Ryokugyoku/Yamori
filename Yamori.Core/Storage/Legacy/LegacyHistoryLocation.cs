namespace Yamori.Core.Storage.Legacy;

/// <summary>
/// 以前の版が使用率の記録（CSV）を置いていたかもしれないフォルダー。
/// </summary>
/// <param name="Directory">CSV を置いていたフォルダー（<c>history</c>）。</param>
/// <param name="ParentsToRemoveIfEmpty">CSV を消したあと、空なら消す親フォルダー（近い順）。以前の版が作ったものだけを入れる。</param>
public sealed record LegacyHistoryLocation(string Directory, IReadOnlyList<string> ParentsToRemoveIfEmpty);
