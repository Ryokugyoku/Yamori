using System.Globalization;

namespace Yamori.Core.Notifications;

/// <summary>
/// 通知に埋め込む「押したときの引数」（トーストの launch 属性）と <see cref="NotificationActivationRequest"/> を相互に変換する。
/// 通知専用の常駐プログラムが作り、アプリ本体が読む。形式は <c>action=anomaly&amp;id=123</c> のような「名前=値」を &amp; でつないだもの。
/// </summary>
public static class NotificationActivationArguments
{
    private const string ActionKey = "action";
    private const string IdKey = "id";
    private const string OpenAppAction = "open";
    private const string AnomalyAction = "anomaly";

    /// <summary>
    /// 開き方の指定を、通知の引数の文字列にする。
    /// </summary>
    /// <param name="request">開き方の指定。</param>
    /// <returns>通知の引数の文字列。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> が null。</exception>
    /// <exception cref="ArgumentException">異常を開く指定なのに、異常の番号がない。</exception>
    public static string Format(NotificationActivationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Kind switch
        {
            NotificationActivationKind.OpenAnomaly when request.AnomalyId is { } id =>
                string.Create(CultureInfo.InvariantCulture, $"{ActionKey}={AnomalyAction}&{IdKey}={id}"),
            NotificationActivationKind.OpenAnomaly =>
                throw new ArgumentException("異常を開く指定には異常の番号が必要です。", nameof(request)),
            _ => $"{ActionKey}={OpenAppAction}",
        };
    }

    /// <summary>
    /// 通知の引数の文字列を読み取る。読めない文字列は「アプリを開くだけ」として扱う。
    /// 古い版や別の版が作った通知を押しても、アプリが開けなくなることがないようにするため。
    /// </summary>
    /// <param name="arguments">通知の引数の文字列。null や空でもよい。</param>
    /// <returns>開き方の指定。</returns>
    public static NotificationActivationRequest Parse(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return NotificationActivationRequest.OpenApp;
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in arguments.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator > 0)
            {
                values[pair[..separator]] = pair[(separator + 1)..];
            }
        }

        if (values.TryGetValue(ActionKey, out var action)
            && string.Equals(action, AnomalyAction, StringComparison.OrdinalIgnoreCase)
            && values.TryGetValue(IdKey, out var idText)
            && long.TryParse(idText, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            return NotificationActivationRequest.ForAnomaly(id);
        }

        return NotificationActivationRequest.OpenApp;
    }
}
