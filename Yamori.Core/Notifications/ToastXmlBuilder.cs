using System.Xml.Linq;

namespace Yamori.Core.Notifications;

/// <summary>
/// Windows の通知（トースト）の中身の XML を作る。見出しと本文の 2 行と、押したときの引数だけを持つ。
/// </summary>
public static class ToastXmlBuilder
{
    /// <summary>
    /// 通知の XML を作る。
    /// </summary>
    /// <param name="title">見出し（1 行目）。</param>
    /// <param name="body">本文（2 行目）。</param>
    /// <param name="activation">押したときにアプリ本体で開くもの。</param>
    /// <returns>トーストの XML の文字列。文字は XML として正しく逃がしてある。</returns>
    /// <exception cref="ArgumentException"><paramref name="title"/> が空。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> か <paramref name="activation"/> が null。</exception>
    public static string Build(string title, string body, NotificationActivationRequest activation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(activation);

        var toast = new XElement(
            "toast",
            new XAttribute("launch", NotificationActivationArguments.Format(activation)),
            new XElement(
                "visual",
                new XElement(
                    "binding",
                    new XAttribute("template", "ToastGeneric"),
                    new XElement("text", title),
                    new XElement("text", body))));

        return toast.ToString(SaveOptions.DisableFormatting);
    }
}
