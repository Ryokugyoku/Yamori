using Yamori.Core.Sensors.Ipc;
using Yamori.Core.Sensors.Ipc.Client;
using Yamori.Core.Sensors.Ipc.Messages;
using Yamori.Core.Sensors.Service;

namespace Yamori.Helper;

/// <summary>
/// GUI が管理者の承認（UAC）を得て起動する、PawnIO の利用の同意を変えるだけのコマンド（<c>Yamori.Helper.exe --pawnio-consent on|off</c>）。
/// 動いている Helper のサービスにパイプでつなぎ、管理者として同意を変える依頼を送って終わる。サービスとしては動かない。
/// </summary>
public static class ConsentCommand
{
    /// <summary>コマンドの名前（引数の 1 つ目）。</summary>
    public const string Name = "--pawnio-consent";

    /// <summary>引数が正しくないときの終了コード。</summary>
    public const int UsageExitCode = 64;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 引数がこのコマンドかどうかを調べる。
    /// </summary>
    /// <param name="args">コマンドラインの引数。</param>
    /// <returns>このコマンドなら true。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> が null。</exception>
    public static bool Matches(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return args.Length > 0 && string.Equals(args[0], Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 同意を変える。
    /// </summary>
    /// <param name="args">コマンドラインの引数（<c>--pawnio-consent on</c> または <c>off</c>）。</param>
    /// <returns>
    /// 終了コード。<see cref="SensorSettingResult"/> の番号（変えられたら 0）。Helper につながらなければ <see cref="SensorSettingResult.Unavailable"/>、
    /// 引数が正しくなければ <see cref="UsageExitCode"/>。
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> が null。</exception>
    public static async Task<int> RunAsync(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        bool enabled;
        switch (args.Length == 2 ? args[1].ToLowerInvariant() : null)
        {
            case "on":
                enabled = true;
                break;
            case "off":
                enabled = false;
                break;
            default:
                Console.Error.WriteLine($"使い方: Yamori.Helper.exe {Name} on|off");
                return UsageExitCode;
        }

        using var timeout = new CancellationTokenSource(Timeout);
        try
        {
            var stream = await SensorPipeConnector.Create(SensorPipeNames.Sensors, Timeout)(timeout.Token);
            await using var client = await SensorPipeClient.ConnectAsync(stream, Timeout, timeout.Token);
            client.Start();
            var payload = IpcConnection.ToPayload(new ConsentMessage(enabled), SensorIpcJsonContext.Default.ConsentMessage);
            var response = await client.RequestAsync(SensorOperation.SetPawnIoConsent, payload, timeout.Token);
            var result = IpcConnection.FromPayload(response.Payload, SensorIpcJsonContext.Default.SettingResultResponse)?.Result
                ?? SensorSettingResult.Unavailable;
            return (int)result;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or OperationCanceledException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Yamori Helper のサービスにつながりませんでした: {ex.Message}");
            return (int)SensorSettingResult.Unavailable;
        }
    }
}
