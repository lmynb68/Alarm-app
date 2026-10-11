$ErrorActionPreference = 'Stop'
try {
    [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
    Write-Host 'AlarmApp 1.0.0 - current-user installation'
    $running = $false
    try { $mutex = [Threading.Mutex]::OpenExisting('Local\IndependentAlarmDesktop'); $mutex.Dispose(); $running = $true } catch [Threading.WaitHandleCannotBeOpenedException] { }
    if ($running) { throw 'Please exit AlarmApp from the tray before installing or updating.' }
    $payload = Join-Path $PSScriptRoot 'app'
    if (-not (Test-Path -LiteralPath $payload)) { Expand-Archive -LiteralPath (Join-Path $PSScriptRoot 'Payload.zip') -DestinationPath $payload }
    $payload = [IO.Path]::GetFullPath($payload).TrimEnd('\')
    $manifest = Get-Content -LiteralPath (Join-Path $payload 'release-manifest.json') -Raw | ConvertFrom-Json
    foreach ($file in $manifest.Files) {
        if ([IO.Path]::IsPathRooted($file.Path) -or $file.Path -match '(^|[\\/])\.\.([\\/]|$)') { throw 'Invalid payload path.' }
        $path = [IO.Path]::GetFullPath((Join-Path $payload $file.Path))
        if (-not $path.StartsWith($payload + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Payload path escaped its directory.' }
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.SHA256) { throw ('Payload verification failed: ' + $file.Path) }
    }
    $root = [IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\AlarmApp')).TrimEnd('\')
    $markerPath = Join-Path $root 'install-marker.json'
    if (Test-Path -LiteralPath $root) {
        if (-not (Test-Path -LiteralPath $markerPath)) { throw 'The install directory exists without an AlarmApp marker. Choose a portable version or resolve the directory manually.' }
        $old = Get-Content -LiteralPath $markerPath -Raw | ConvertFrom-Json
        if ($old.ProductId -ne 'AlarmApp-9A44CD84-E036-4A31-9177-35986BE23D4B') { throw 'Installation marker mismatch.' }
    }
    $reg = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AlarmApp'
    if (Test-Path -LiteralPath $reg) { $previous = Get-ItemProperty -LiteralPath $reg; if ($previous.InstallLocation -ne $root) { throw 'The uninstall registry key belongs to another location.' } }
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    foreach ($file in $manifest.Files) {
        $dest = [IO.Path]::GetFullPath((Join-Path $root $file.Path))
        if (-not $dest.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Destination path escaped the app directory.' }
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($dest)) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $payload $file.Path) -Destination $dest -Force
    }
    Copy-Item -LiteralPath (Join-Path $payload 'release-manifest.json') -Destination (Join-Path $root 'release-manifest.json') -Force
    @{ ProductId='AlarmApp-9A44CD84-E036-4A31-9177-35986BE23D4B'; Version=$manifest.Version; InstalledAt=(Get-Date -Format o) } | ConvertTo-Json | Out-File -LiteralPath $markerPath -Encoding utf8
    $exe = Join-Path $root 'AlarmApp.exe'
    $icon = Join-Path $root 'AppIcon-v1.ico'
    $shell = New-Object -ComObject WScript.Shell
    $desktop = [Environment]::GetFolderPath('DesktopDirectory')
    $programs = [Environment]::GetFolderPath('Programs')
    foreach ($link in @((Join-Path $desktop 'AlarmApp.lnk'), (Join-Path $programs 'AlarmApp.lnk'))) {
        if (Test-Path -LiteralPath $link) {
            $existing = $shell.CreateShortcut($link)
            if ($existing.TargetPath -ne $exe) { Write-Warning 'A shortcut with another target was kept.'; continue }
        }
        $shortcut = $shell.CreateShortcut($link)
        $shortcut.TargetPath = $exe
        $shortcut.WorkingDirectory = $root
        $shortcut.IconLocation = $icon + ',0'
        $shortcut.Description = 'AlarmApp - scheduled and interval reminders'
        $shortcut.Save()
    }
    $powershell = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $root 'Uninstall.ps1') + '"'
    $uninstallLink = Join-Path $programs 'Uninstall AlarmApp.lnk'
    $canWriteUninstallLink = $true
    if (Test-Path -LiteralPath $uninstallLink) { $existing = $shell.CreateShortcut($uninstallLink); $canWriteUninstallLink = $existing.Arguments.Contains((Join-Path $root 'Uninstall.ps1')) }
    if ($canWriteUninstallLink) { $shortcut=$shell.CreateShortcut($uninstallLink); $shortcut.TargetPath=$powershell; $shortcut.Arguments=$arguments; $shortcut.IconLocation=$icon+',0'; $shortcut.Save() }
    New-Item -Path $reg -Force | Out-Null
    $properties = @{ DisplayName='AlarmApp'; DisplayVersion=$manifest.Version; Publisher='AlarmApp Project'; InstallLocation=$root; DisplayIcon=$icon; UninstallString=('"'+$powershell+'" '+$arguments); NoModify=1; NoRepair=1 }
    foreach ($name in $properties.Keys) { $kind=if($name -in @('NoModify','NoRepair')){'DWord'}else{'String'}; New-ItemProperty -Path $reg -Name $name -Value $properties[$name] -PropertyType $kind -Force | Out-Null }
    Write-Host 'Installed. Use AlarmApp from the desktop or Start menu.'
    Start-Process -FilePath $exe
    Read-Host 'Press Enter to close this installer'
} catch {
    ($_ | Out-String) | Out-File -LiteralPath (Join-Path $PSScriptRoot 'install-error.log') -Encoding utf8
    Write-Host 'Installation failed. See install-error.log.' -ForegroundColor Red
    Write-Host $_.Exception.Message
    Read-Host 'Press Enter to close'
    exit 1
}
