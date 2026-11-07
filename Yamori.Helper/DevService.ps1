<#
.SYNOPSIS
    開発中に Yamori Helper を sc.exe で Windows サービス（LocalSystem）として登録・起動・停止・削除する。管理者の PowerShell から実行すること。

.DESCRIPTION
    MSIX から入れた Helper（サービス名 YamoriHelper）とは別の名前（YamoriHelperDev）で登録する。
    同じパイプを使うため、両方を同時には動かせない（後から起動したほうは、パイプを作れずに止まる）。

.EXAMPLE
    .\DevService.ps1 -Action Install
    .\DevService.ps1 -Action Stop
    .\DevService.ps1 -Action Uninstall
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Install', 'Start', 'Stop', 'Uninstall')]
    [string] $Action,

    [string] $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$serviceName = 'YamoriHelperDev'
$exe = Join-Path $PSScriptRoot "bin\x64\$Configuration\net10.0-windows\win-x64\Yamori.Helper.exe"

switch ($Action) {
    'Install' {
        if (-not (Test-Path $exe)) { throw "先に Yamori.Helper をビルドしてください: $exe" }
        sc.exe create $serviceName binPath= "`"$exe`"" start= demand obj= LocalSystem DisplayName= "Yamori Helper (dev)"
        sc.exe start $serviceName
    }
    'Start' { sc.exe start $serviceName }
    'Stop' { sc.exe stop $serviceName }
    'Uninstall' {
        sc.exe stop $serviceName | Out-Null
        sc.exe delete $serviceName
    }
}
