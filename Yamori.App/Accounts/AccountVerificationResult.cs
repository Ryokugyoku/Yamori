namespace Yamori.Accounts;

/// <summary>
/// 本人確認の結果。
/// </summary>
public enum AccountVerificationResult
{
    /// <summary>本人であることを確認できた。</summary>
    Verified,

    /// <summary>Windows Hello が使えない（未設定、デバイスなし、ポリシーで無効）。</summary>
    Unavailable,

    /// <summary>ユーザーがキャンセルした。</summary>
    Canceled,

    /// <summary>確認できなかった（認証の失敗、試行回数の超過など）。</summary>
    Failed,
}
