namespace Yamori.Views;

/// <summary>
/// 自分の中でスクロールする画面の目印。メイン画面は、この画面を表示している間は外側のスクロールを止め、画面を窓の高さに合わせる。
/// </summary>
/// <remarks>左の一覧を固定したまま、右の詳細だけをスクロールしたい画面（リアルタイム画面など）に付ける。</remarks>
public interface ISelfScrollingPage
{
}
