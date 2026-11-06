using System.Threading.Tasks;

namespace Yamori.Services;

/// <summary>
/// ユーザーにフォルダーを選んでもらう。
/// </summary>
public interface IFolderPicker
{
    /// <summary>
    /// フォルダーの選択画面を表示する。
    /// </summary>
    /// <returns>選ばれたフォルダーの絶対パス。キャンセルされたら null。</returns>
    Task<string?> PickFolderAsync();
}
