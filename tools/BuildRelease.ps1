$ErrorActionPreference = 'Stop'
try {
    [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
    $repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $artifacts = Join-Path $repo 'artifacts'
    $stage = Join-Path $artifacts ('staging\' + [Guid]::NewGuid().ToString('N'))
    $portable = Join-Path $stage 'AlarmApp'
    $setup = Join-Path $stage 'AlarmApp-Setup'
    $payload = Join-Path $stage 'Payload'
    New-Item -ItemType Directory -Path $portable,$setup,$payload -Force | Out-Null
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
    if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework compiler is missing.' }
    $source = Join-Path $repo 'src\AlarmApp\Alarm.cs'
    $assembly = Join-Path $repo 'src\AlarmApp\Properties\AssemblyInfo.cs'
    $exe = Join-Path $portable 'AlarmApp.exe'
    $icon = Join-Path $repo 'assets\AppIcon-v1.ico'
    $quote = [char]34
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $compiler
    $info.Arguments = '/nologo /utf8output /codepage:65001 /target:winexe /optimize+ /out:'+$quote+$exe+$quote+' /win32icon:'+$quote+$icon+$quote+' /reference:System.Windows.Forms.dll /reference:System.Drawing.dll '+$quote+$source+$quote+' '+$quote+$assembly+$quote
    $info.UseShellExecute=$false; $info.CreateNoWindow=$true; $info.RedirectStandardOutput=$true; $info.RedirectStandardError=$true
    $info.StandardOutputEncoding=New-Object System.Text.UTF8Encoding($false)
    $info.StandardErrorEncoding=New-Object System.Text.UTF8Encoding($false)
    $process=New-Object System.Diagnostics.Process; $process.StartInfo=$info; [void]$process.Start()
    $outputTask=$process.StandardOutput.ReadToEndAsync(); $errorTask=$process.StandardError.ReadToEndAsync(); $process.WaitForExit()
    ($outputTask.Result+$errorTask.Result) | Out-File -LiteralPath (Join-Path $artifacts 'build.log') -Encoding utf8
    if ($process.ExitCode -ne 0) { throw 'Compilation failed. See artifacts/build.log.' }
    $process.Dispose()
    foreach ($name in @('AppIcon-v1.ico','AppIcon.png','alarm.wav')) { Copy-Item -LiteralPath (Join-Path $repo ('assets\'+$name)) -Destination (Join-Path $portable $name) }
    Copy-Item -LiteralPath (Join-Path $repo 'assets\sounds') -Destination (Join-Path $portable 'sounds') -Recurse
    Copy-Item -LiteralPath (Join-Path $repo 'docs\USER_README.txt') -Destination (Join-Path $portable 'README.txt')
    Write-Host 'Running application self-tests...'
    $test=Start-Process -FilePath $exe -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
    if ($test.ExitCode -ne 0) { throw 'Self-tests failed; release archives were not generated.' }
    Copy-Item -LiteralPath (Join-Path $portable 'verification.txt') -Destination (Join-Path $artifacts 'verification.txt') -Force
    Remove-Item -LiteralPath (Join-Path $portable 'verification.txt')
    foreach ($item in (Get-ChildItem -LiteralPath $portable -Force)) { Copy-Item -LiteralPath $item.FullName -Destination (Join-Path $payload $item.Name) -Recurse }
    foreach ($name in @('Uninstall.ps1','Uninstall.cmd')) { Copy-Item -LiteralPath (Join-Path $repo ('installer\'+$name)) -Destination (Join-Path $payload $name) }
    foreach ($folder in @($portable,$payload)) {
        $files=@(Get-ChildItem -LiteralPath $folder -File -Recurse | Sort-Object FullName | ForEach-Object { @{ Path=$_.FullName.Substring($folder.Length+1).Replace('\','/'); SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(); Size=$_.Length } })
        @{ Product='AlarmApp'; Version='1.0.0'; SourceSHA256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant(); Verification='Application self-tests passed'; Files=$files } | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $folder 'release-manifest.json') -Encoding utf8
    }
    foreach ($name in @('Install.ps1','Install.cmd')) { Copy-Item -LiteralPath (Join-Path $repo ('installer\'+$name)) -Destination (Join-Path $setup $name) }
    Copy-Item -LiteralPath (Join-Path $repo 'docs\USER_README.txt') -Destination (Join-Path $setup 'README.txt')
    Compress-Archive -Path (Join-Path $payload '*') -DestinationPath (Join-Path $setup 'Payload.zip')
    Compress-Archive -LiteralPath $portable -DestinationPath (Join-Path $artifacts 'AlarmApp-1.0.0-Portable.zip') -Force
    Compress-Archive -LiteralPath $setup -DestinationPath (Join-Path $artifacts 'AlarmApp-1.0.0-Setup.zip') -Force
    @('AlarmApp-1.0.0-Portable.zip','AlarmApp-1.0.0-Setup.zip') | ForEach-Object { $path=Join-Path $artifacts $_; ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$_) } | Out-File -LiteralPath (Join-Path $artifacts 'SHA256SUMS.txt') -Encoding ascii
    Write-Host 'Release archives are ready in artifacts/.'
} catch {
    Write-Host ('Build failed: '+$_.Exception.Message) -ForegroundColor Red
    exit 1
}
