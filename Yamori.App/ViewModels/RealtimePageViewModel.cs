using System;
using Yamori.ViewModels.Sensors;

namespace Yamori.ViewModels;

/// <summary>
/// リアルタイム画面の状態。センサーの詳細のツリー（カタログから自動で組み立てる）と、アプリごとの今の使用量を持つ。
/// </summary>
public sealed class RealtimePageViewModel : IDisposable
{
    /// <summary>
    /// 画面の部品の状態を指定して作る。
    /// </summary>
    /// <param name="tree">センサーの詳細のツリー。このインスタンスが破棄する。</param>
    /// <param name="apps">アプリごとの今の使用量。このインスタンスが破棄する。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public RealtimePageViewModel(SensorTreeViewModel tree, RealtimeViewModel apps)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(apps);
        Tree = tree;
        Apps = apps;
    }

    /// <summary>センサーの詳細のツリーを取得する。</summary>
    public SensorTreeViewModel Tree { get; }

    /// <summary>アプリごとの今の使用量を取得する。センサーではないため、従来の測定のまま表示する。</summary>
    public RealtimeViewModel Apps { get; }

    /// <summary>
    /// 画面を開いたときに呼ぶ。アプリごとの使用量の測定を始める。
    /// </summary>
    public void Start() => Apps.Start();

    /// <summary>
    /// 測定を止め、購読を解除する。
    /// </summary>
    public void Dispose()
    {
        Apps.Dispose();
        Tree.Dispose();
    }
}
