using System;
using Yamori.Localization;

namespace Yamori.Presentation;

/// <summary>
/// 概要画面の一番上に出す、時間帯に合わせたあいさつ。言語は現在の UI カルチャに従う。
/// </summary>
public static class GreetingFormatter
{
    /// <summary>
    /// あいさつを作る。5〜11 時は朝、11〜18 時は昼、それ以外は夜のあいさつにする。
    /// </summary>
    /// <param name="localTime">今の時刻（この PC の時刻）。</param>
    /// <param name="userName">ユーザーの名前。</param>
    /// <returns>あいさつの文。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="userName"/> が null。</exception>
    public static string For(DateTimeOffset localTime, string userName)
    {
        ArgumentNullException.ThrowIfNull(userName);
        var key = localTime.Hour switch
        {
            >= 5 and < 11 => "Greeting_Morning",
            >= 11 and < 18 => "Greeting_Afternoon",
            _ => "Greeting_Evening",
        };
        return UiText.Format(key, userName);
    }
}
