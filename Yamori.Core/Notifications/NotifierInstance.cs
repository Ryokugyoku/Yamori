namespace Yamori.Core.Notifications;

/// <summary>
/// 通知専用の常駐プログラム（Yamori.Notifier）の名前と、動いているかの確かめ方。アプリ本体と常駐プログラムの両方が使う。
/// </summary>
public static class NotifierInstance
{
    /// <summary>常駐プログラムの実行ファイルの名前。アプリ本体と同じフォルダーに置く。</summary>
    public const string ExecutableName = "Yamori.Notifier.exe";

    /// <summary>
    /// 1 つのサインインの中で常駐プログラムを 1 つだけにするためのミューテックスの名前。
    /// Local\ を付け、サインインごと（セッションごと）に別にする。
    /// </summary>
    public const string MutexName = @"Local\Yamori.Notifier";

    /// <summary>
    /// 常駐プログラムが、今のサインインの中で動いているかどうかを調べる。
    /// </summary>
    /// <returns>動いているなら true。</returns>
    public static bool IsRunning()
    {
        if (Mutex.TryOpenExisting(MutexName, out var mutex))
        {
            mutex.Dispose();
            return true;
        }

        return false;
    }
}
