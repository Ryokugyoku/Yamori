namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// Windows 全体のメモリの使い方と、プロセス・スレッド・ハンドルの数を読み取る。
/// </summary>
public interface ISystemPerformanceSource
{
    /// <summary>
    /// 今の値を読み取る。
    /// </summary>
    /// <returns>今の値。読めない場合は null。</returns>
    SystemPerformance? Read();
}
