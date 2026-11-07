<#
.SYNOPSIS
    開発中に Yamori Helper を sc.exe で Windows サービス（LocalSystem）として登録・同期・起動・停止・削除する。

.DESCRIPTION
    ビルドの出力を %ProgramData%\YamoriDev\Helper にコピーし、そこからサービスを動かす。
    ビルドの出力のフォルダーから直接動かすと、Visual Studio で GUI をビルドし直すたびに（GUI は Helper を参照している）ファイルが使用中で失敗するため。

    Install（管理者で実行）は、登録した人のアカウントに、このサービスの停止・起動と、コピー先のフォルダーへの書き込みを許可する。
    これにより、Debug のビルドの最後に呼ばれる Sync（管理者でなくてよい）が、変わったファイルだけをコピーしてサービスを再起動できる。
    注意：許可したアカウントで動くプログラムは、UAC なしに SYSTEM で動くコードを差し替えられる。開発用の PC だけで使い、使い終わったら Uninstall すること。

    MSIX の Release のパッケージの Helper（サービス名 YamoriHelper）とは別の名前（YamoriHelperDev）で登録する。
    同じパイプを使うため、両方を同時には動かせない（後から起動したほうは、パイプを作れずに止まる）。

.EXAMPLE
    .\DevService.ps1 -Action Install     # 管理者
    .\DevService.ps1 -Action Sync        # ビルドの最後に自動で呼ばれる（管理者でなくてよい）
    .\DevService.ps1 -Action Uninstall   # 管理者
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Install', 'Sync', 'Update', 'Start', 'Stop', 'Uninstall')]
    [string] $Action,

    [string] $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$serviceName = 'YamoriHelperDev'
$buildOutput = Join-Path $PSScriptRoot "bin\x64\$Configuration\net10.0-windows\win-x64"
$serviceFolder = Join-Path $env:ProgramData 'YamoriDev\Helper'
$exe = Join-Path $serviceFolder 'Yamori.Helper.exe'

function Test-DevServiceInstalled { [bool](Get-Service -Name $serviceName -ErrorAction SilentlyContinue) }

function Stop-DevService {
    if (Test-DevServiceInstalled) {
        $service = Get-Service -Name $serviceName
        if ($service.Status -ne 'Stopped') {
            $service.Stop()
            $service.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
        }
    }
}

function Start-DevService {
    $service = Get-Service -Name $serviceName
    $service.Start()
    $service.WaitForStatus('Running', [TimeSpan]::FromSeconds(30))
}

function Assert-BuildOutput {
    if (-not (Test-Path (Join-Path $buildOutput 'Yamori.Helper.exe'))) {
        throw "先に Yamori.Helper をビルドしてください: $buildOutput"
    }
}

# ビルドの出力のうち、コピー先と大きさか更新日時が違うファイルを返す。
function Get-ChangedFiles {
    Get-ChildItem -Path $buildOutput -Recurse -File | Where-Object {
        $target = Join-Path $serviceFolder $_.FullName.Substring($buildOutput.Length).TrimStart('\')
        -not (Test-Path $target) -or (Get-Item $target).Length -ne $_.Length -or (Get-Item $target).LastWriteTimeUtc -ne $_.LastWriteTimeUtc
    }
}

function Copy-Files($files) {
    foreach ($file in $files) {
        $target = Join-Path $serviceFolder $file.FullName.Substring($buildOutput.Length).TrimStart('\')
        New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
        Copy-Item -Path $file.FullName -Destination $target -Force
    }
}

# 登録した人のアカウントに、サービスの停止・起動（RP/WP）と状態の取得（LC/RC）を許可する。
function Grant-ServiceControl([string] $sid) {
    $sddl = (sc.exe sdshow $serviceName | Where-Object { $_ -match '\S' }) -join ''
    $ace = "(A;;RPWPLCRC;;;$sid)"
    if ($sddl -notlike "*$ace*") {
        $sddl = if ($sddl -match 'S:') { $sddl -replace 'S:', "${ace}S:" } else { $sddl + $ace }
        sc.exe sdset $serviceName $sddl | Out-Null
    }
}

function Grant-FolderWrite([string] $sid) {
    $acl = Get-Acl $serviceFolder
    $rule = New-Object Security.AccessControl.FileSystemAccessRule(
        (New-Object Security.Principal.SecurityIdentifier $sid), 'Modify', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
    $acl.AddAccessRule($rule)
    Set-Acl -Path $serviceFolder -AclObject $acl
}

switch ($Action) {
    'Install' {
        Assert-BuildOutput
        $sid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
        if (Test-Path $serviceFolder) { Remove-Item -Recurse -Force $serviceFolder }
        New-Item -ItemType Directory -Force $serviceFolder | Out-Null
        Copy-Files (Get-ChildItem -Path $buildOutput -Recurse -File)
        sc.exe create $serviceName binPath= "`"$exe`"" start= demand obj= LocalSystem DisplayName= "Yamori Helper (dev)"
        Grant-ServiceControl $sid
        Grant-FolderWrite $sid
        Start-DevService
        Write-Host "登録しました。Debug で Yamori.Helper をビルドすると、自動で $serviceName に反映されます。"
    }
    { $_ -in 'Sync', 'Update' } {
        # 開発用のサービスを登録していない PC（CI やほかの開発者）では何もしない。
        if (-not (Test-DevServiceInstalled)) { return }
        Assert-BuildOutput
        $changed = @(Get-ChangedFiles)
        if ($changed.Count -eq 0) {
            Write-Host "Yamori Helper (dev): 変更なし"
            return
        }

        Stop-DevService
        Copy-Files $changed
        Start-DevService
        Write-Host "Yamori Helper (dev): $($changed.Count) 個のファイルを反映して再起動しました"
    }
    'Start' { Start-DevService }
    'Stop' { Stop-DevService }
    'Uninstall' {
        Stop-DevService
        sc.exe delete $serviceName
        if (Test-Path $serviceFolder) { Remove-Item -Recurse -Force $serviceFolder }
    }
}
