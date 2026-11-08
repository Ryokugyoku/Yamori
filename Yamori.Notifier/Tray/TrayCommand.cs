namespace Yamori.Notifier.Tray;

/// <summary>
/// トレイのメニューの項目。値はメニューの項目の番号に使うため、0 は使わない（0 は「何も選ばなかった」）。
/// </summary>
internal enum TrayCommand
{
    /// <summary>Yamori（アプリ本体）を開く。</summary>
    OpenApp = 1,

    /// <summary>通知を 1 時間止める。</summary>
    PauseOneHour = 2,

    /// <summary>通知を明日まで止める。</summary>
    PauseUntilTomorrow = 3,

    /// <summary>止めた通知を再開する。</summary>
    Resume = 4,

    /// <summary>通知専用の常駐プログラムを終える。</summary>
    Exit = 5,
}
