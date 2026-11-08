using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Yamori.Services;

/// <summary>
/// Windows の <c>sc.exe start</c> で Helper のサービスを起動する <see cref="IHelperServiceStarter"/>。
/// まず今の権限で試し、権限が足りなければ、同じコマンドを管理者の承認（UAC）つきで起動する。
/// </summary>
public sealed class ScHelperServiceStarter : IHelperServiceStarter
{
    /// <summary>成功。</summary>
    private const int Success = 0;

    /// <summary>権限が足りない（ERROR_ACCESS_DENIED）。</summary>
    private const int AccessDenied = 5;

    /// <summary>もう動いている（ERROR_SERVICE_ALREADY_RUNNING）。</summary>
    private const int AlreadyRunning = 1056;

    /// <summary>無効にされている（ERROR_SERVICE_DISABLED）。</summary>
    private const int ServiceDisabled = 1058;

    /// <summary>登録されていない（ERROR_SERVICE_DOES_NOT_EXIST）。</summary>
    private const int DoesNotExist = 1060;

    /// <summary>承認の画面で「いいえ」を選ばれた（ERROR_CANCELLED）。</summary>
    private const int ErrorCancelled = 1223;

    private readonly string _serviceName;
    private readonly string _scPath;

    /// <summary>
    /// 起動するサービスの名前を指定して作る。
    /// </summary>
    /// <param name="serviceName">サービスの名前。省略時は <see cref="HelperServiceName.Current"/>。</param>
    public ScHelperServiceStarter(string? serviceName = null)
    {
        _serviceName = serviceName ?? HelperServiceName.Current;
        _scPath = Path.Combine(Environment.SystemDirectory, "sc.exe");
    }

    /// <inheritdoc />
    public async Task<HelperStartResult> StartAsync()
    {
        int? exitCode = await RunAsync(elevated: false);
        if (exitCode == AccessDenied)
        {
            exitCode = await RunAsync(elevated: true);
        }

        return exitCode switch
        {
            null => HelperStartResult.Cancelled,
            Success or AlreadyRunning => HelperStartResult.Started,
            DoesNotExist => HelperStartResult.NotInstalled,
            ServiceDisabled => HelperStartResult.Disabled,
            _ => HelperStartResult.Failed,
        };
    }

    /// <summary>
    /// <c>sc.exe start</c> を起動し、終わるまで待つ。
    /// </summary>
    /// <param name="elevated">管理者の承認を得て起動するなら true。</param>
    /// <returns>終了コード。承認の画面で取り消された場合は null。起動できなかった場合は -1。</returns>
    private async Task<int?> RunAsync(bool elevated)
    {
        var start = new ProcessStartInfo(_scPath)
        {
            Arguments = $"start \"{_serviceName}\"",
            UseShellExecute = elevated,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        if (elevated)
        {
            start.Verb = "runas";
        }
        else
        {
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
        }

        Process? process;
        try
        {
            process = Process.Start(start);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            return null;
        }
        catch (Win32Exception)
        {
            return -1;
        }

        if (process is null)
        {
            return -1;
        }

        using (process)
        {
            if (!elevated)
            {
                // 出力を読み捨てないと、出力が多いときにバッファーが詰まって終わらなくなる。
                _ = process.StandardOutput.ReadToEndAsync();
                _ = process.StandardError.ReadToEndAsync();
            }

            await process.WaitForExitAsync();
            return process.ExitCode;
        }
    }
}
