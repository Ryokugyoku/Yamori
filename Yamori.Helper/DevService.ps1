<#
.SYNOPSIS
    開発中に Yamori Helper を sc.exe で Windows サービス（LocalSystem）として登録・更新・起動・停止・削除する。管理者の PowerShell から実行すること。

.DESCRIPTION
    ビルドの出力を %ProgramData%\YamoriDev\Helper にコピーし、そこからサービスを動かす。
    ビルドの出力のフォルダーから直接動かすと、Visual Studio で GUI をビルドし直すたびに（GUI は Helper を参照している）ファイルが使用中で失敗するため。
    Helper のコードを変えたら、ビルドしてから -Action Update でコピーし直す。

    MSIX から入れた Helper（サービス名 YamoriHelper）とは別の名前（YamoriHelperDev）で登録する。
    同じパイプを使うため、両方を同時には動かせない（後から起動したほうは、パイプを作れずに止まる）。

.EXAMPLE
    .\DevService.ps1 -Action Install
    .\DevService.ps1 -Action Update
    .\DevService.ps1 -Action Stop
    .\DevService.ps1 -Action Uninstall
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Install', 'Update', 'Start', 'Stop', 'Uninstall')]
    [string] $Action,

    [string] $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$serviceName = 'YamoriHelperDev'
$buildOutput = Join-Path $PSScriptRoot "bin\x64\$Configuration\net10.0-windows\win-x64"
$serviceFolder = Join-Path $env:ProgramData 'YamoriDev\Helper'
$exe = Join-Path $serviceFolder 'Yamori.Helper.exe'

function Stop-DevService {
    if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
        Stop-Service -Name $serviceName -ErrorAction SilentlyContinue
        (Get-Service -Name $serviceName).WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
    }
}

function Copy-BuildOutput {
    if (-not (Test-Path (Join-Path $buildOutput 'Yamori.Helper.exe'))) {
        throw "先に Yamori.Helper をビルドしてください: $buildOutput"
    }

    if (Test-Path $serviceFolder) { Remove-Item -Recurse -Force $serviceFolder }
    New-Item -ItemType Directory -Force $serviceFolder | Out-Null
    Copy-Item -Path (Join-Path $buildOutput '*') -Destination $serviceFolder -Recurse -Force
}

switch ($Action) {
    'Install' {
        Copy-BuildOutput
        sc.exe create $serviceName binPath= "`"$exe`"" start= demand obj= LocalSystem DisplayName= "Yamori Helper (dev)"
        sc.exe start $serviceName
    }
    'Update' {
        Stop-DevService
        Copy-BuildOutput
        sc.exe config $serviceName binPath= "`"$exe`""
        sc.exe start $serviceName
    }
    'Start' { sc.exe start $serviceName }
    'Stop' { Stop-DevService }
    'Uninstall' {
        Stop-DevService
        sc.exe delete $serviceName
        if (Test-Path $serviceFolder) { Remove-Item -Recurse -Force $serviceFolder }
    }
}
