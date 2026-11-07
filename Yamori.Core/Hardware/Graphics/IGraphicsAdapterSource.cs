namespace Yamori.Core.Hardware.Graphics;

/// <summary>
/// GPU の詳しい情報（正確なビデオメモリ、内蔵かどうか）を提供する。テストで差し替えられるように抽象化している。
/// </summary>
public interface IGraphicsAdapterSource
{
    /// <summary>
    /// ハードウェアの GPU の一覧を取得する。ソフトウェアで描画するアダプターは含まない。
    /// </summary>
    /// <returns>GPU の一覧。取得できない場合は空。</returns>
    IReadOnlyList<GraphicsAdapterDetails> GetAdapters();
}
