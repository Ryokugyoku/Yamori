namespace Yamori.Services;

/// <summary>
/// Helper のサービスを起動しようとした結果。
/// </summary>
public enum HelperStartResult
{
    /// <summary>起動を始めた（または、もう動いていた）。接続は自動でつなぎ直される。</summary>
    Started = 0,

    /// <summary>管理者の承認の画面で取り消された。</summary>
    Cancelled = 1,

    /// <summary>サービスが登録されていない。</summary>
    NotInstalled = 2,

    /// <summary>サービスが無効にされている。</summary>
    Disabled = 3,

    /// <summary>そのほかの理由で起動できなかった。</summary>
    Failed = 4,
}
