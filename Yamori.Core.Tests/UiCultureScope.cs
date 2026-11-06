using System.Globalization;

namespace Yamori.Core.Tests;

/// <summary>
/// テストの間だけ、現在のスレッドの UI カルチャを差し替える。
/// </summary>
internal sealed class UiCultureScope : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentUICulture;

    /// <summary>
    /// 指定したカルチャに切り替える。
    /// </summary>
    /// <param name="cultureName">カルチャ名（例：ja-JP）。</param>
    public UiCultureScope(string cultureName) => CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

    /// <summary>
    /// 元のカルチャに戻す。
    /// </summary>
    public void Dispose() => CultureInfo.CurrentUICulture = _previous;
}
