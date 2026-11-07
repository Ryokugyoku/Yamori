using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Yamori.Core.Sensors.Service;

namespace Yamori.Services;

/// <summary>
/// パッケージに同梱した <c>Yamori.Helper.exe</c> を、同意を変えるコマンド（<c>--pawnio-consent on|off</c>）付きで管理者として起動する
/// <see cref="IElevatedConsentChanger"/>。起動したコマンドは、動いている Helper にパイプで変更を依頼して、すぐに終わる。
/// </summary>
public sealed class HelperElevatedConsentChanger : IElevatedConsentChanger
{
    /// <summary>承認の画面で「いいえ」を選ばれたときのエラー（ERROR_CANCELLED）。</summary>
    private const int ErrorCancelled = 1223;

    private readonly string _helperPath;

    /// <summary>
    /// Helper の実行ファイルの場所を指定して作る。
    /// </summary>
    /// <param name="helperPath">Helper の実行ファイルのパス。省略時は GUI と同じフォルダーの <c>Yamori.Helper.exe</c>。</param>
    public HelperElevatedConsentChanger(string? helperPath = null)
    {
        _helperPath = helperPath ?? Path.Combine(AppContext.BaseDirectory, "Yamori.Helper.exe");
    }

    /// <inheritdoc />
    public async Task<SensorSettingResult?> SetPawnIoConsentAsync(bool enabled)
    {
        if (!File.Exists(_helperPath))
        {
            return SensorSettingResult.Unavailable;
        }

        var start = new ProcessStartInfo(_helperPath)
        {
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = enabled ? "--pawnio-consent on" : "--pawnio-consent off",
        };

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
            return SensorSettingResult.Unavailable;
        }

        if (process is null)
        {
            return SensorSettingResult.Unavailable;
        }

        using (process)
        {
            await process.WaitForExitAsync();
            return Enum.IsDefined(typeof(SensorSettingResult), process.ExitCode)
                ? (SensorSettingResult)process.ExitCode
                : SensorSettingResult.Unavailable;
        }
    }
}
