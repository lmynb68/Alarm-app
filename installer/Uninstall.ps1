$ErrorActionPreference = 'Stop'
try {
    $root = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')
    $expected = [IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\AlarmApp')).TrimEnd('\')
    if ($root -ne $expected) { throw 'Unexpected installation directory. Nothing was removed.' }
    $markerPath = Join-Path $root 'install-marker.json'
    $marker = Get-Content -LiteralPath $markerPath -Raw | ConvertFrom-Json
    if ($marker.ProductId -ne 'AlarmApp-9A44CD84-E036-4A31-9177-35986BE23D4B') { throw 'Installation marker mismatch. Nothing was removed.' }
    $running = $false
    try { $mutex = [Threading.Mutex]::OpenExisting('Local\IndependentAlarmDesktop'); $mutex.Dispose(); $running = $true } catch [Threading.WaitHandleCannotBeOpenedException] { }
    if ($running) { throw 'Please exit AlarmApp from the tray before uninstalling.' }
    $exe = Join-Path $root 'AlarmApp.exe'
    $shell = New-Object -ComObject WScript.Shell
    $desktop = [Environment]::GetFolderPath('DesktopDirectory')
    $programs = [Environment]::GetFolderPath('Programs')
    foreach ($link in @((Join-Path $desktop 'AlarmApp.lnk'), (Join-Path $programs 'AlarmApp.lnk'), (Join-Path $programs 'Uninstall AlarmApp.lnk'))) {
        if (Test-Path -LiteralPath $link) {
            $item = $shell.CreateShortcut($link)
            if ($item.TargetPath -eq $exe -or ($item.TargetPath -eq (Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe') -and $item.Arguments.Contains((Join-Path $root 'Uninstall.ps1')))) { Remove-Item -LiteralPath $link -Force }
        }
    }
    $reg = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AlarmApp'
    if (Test-Path -LiteralPath $reg) { $values = Get-ItemProperty -LiteralPath $reg; if ($values.InstallLocation -eq $root) { Remove-Item -LiteralPath $reg } }
    Set-Location -LiteralPath ([IO.Path]::GetTempPath())
    # Recursive deletion is allowed only after the exact absolute app path and product marker checks above.
    Remove-Item -LiteralPath $root -Recurse -Force
    Write-Host 'AlarmApp uninstalled. Your alarm settings and history have been kept.'
    Read-Host 'Press Enter to close'
} catch {
    Write-Host ('Uninstall failed: ' + $_.Exception.Message) -ForegroundColor Red
    Read-Host 'Press Enter to close'
    exit 1
}
