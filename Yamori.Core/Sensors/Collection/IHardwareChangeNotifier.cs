namespace Yamori.Core.Sensors.Collection;

/// <summary>
/// ハードウェアの構成が変わったこと（デバイスの追加・取り外し）を知らせる。センサーの宣言をやり直すきっかけに使う。
/// </summary>
public interface IHardwareChangeNotifier
{
    /// <summary>
    /// デバイスが追加または取り外されたときに発生する。続けて何度も発生することがあり、任意のスレッドから発生する。
    /// </summary>
    event EventHandler? Changed;
}
