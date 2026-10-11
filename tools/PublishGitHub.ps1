param([string]$RepositoryName='Alarm-app', [string]$ExpectedOwner='lmynb68')
$ErrorActionPreference = 'Stop'
try {
    if ($RepositoryName -notmatch '^[A-Za-z0-9_.-]+$') { throw 'Invalid repository name.' }
    $repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $artifacts = Join-Path $repo 'artifacts'
    $portable = Join-Path $artifacts 'AlarmApp-1.0.0-Portable.zip'
    $setup = Join-Path $artifacts 'AlarmApp-1.0.0-Setup.zip'
    $checksums = Join-Path $artifacts 'SHA256SUMS.txt'
    foreach ($file in @($portable,$setup,$checksums)) { if (-not (Test-Path -LiteralPath $file)) { throw 'Release files are missing. Run Build.cmd first.' } }
    $cli = Get-Command gh.exe -ErrorAction SilentlyContinue
    if (-not $cli) { throw 'GitHub CLI is required for publishing. Download it from https://cli.github.com/ or publish the files on GitHub manually.' }
    & $cli.Source auth status 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) { & $cli.Source auth login; if ($LASTEXITCODE -ne 0) { throw 'GitHub sign-in failed.' } }
    $login = & $cli.Source api user --jq .login
    if ($LASTEXITCODE -ne 0) { throw 'Could not determine the signed-in GitHub account.' }
    if ($login -ne $ExpectedOwner) { throw ('Signed in as '+$login+', expected '+$ExpectedOwner+'. Switch accounts or set ExpectedOwner explicitly.') }
    $full = $login + '/' + $RepositoryName
    Write-Host ('Publishing binary download packages to https://github.com/'+$full)
    $createdRepository = $false
    $metadataText = & $cli.Source api ('repos/'+$full) 2>$null
    if ($LASTEXITCODE -ne 0) {
        $createdRepository = $true
        $create = @{ name=$RepositoryName; description='Standalone Windows scheduled and interval alarm app.'; private=$false; auto_init=$true } | ConvertTo-Json
        $request = Join-Path $artifacts 'publish-repository-request.json'
        [IO.File]::WriteAllText($request,$create,(New-Object Text.UTF8Encoding($false)))
        & $cli.Source api user/repos --method POST --input $request 1>$null
        if ($LASTEXITCODE -ne 0) { throw 'Could not create the public download repository.' }
    } else {
        $metadata = $metadataText | ConvertFrom-Json
        if ($metadata.private) { throw 'The existing repository is private; it will not be converted automatically.' }
        $readme = & $cli.Source api ('repos/'+$full+'/readme') --jq .content
        if ($LASTEXITCODE -ne 0) { throw 'An existing repository has no AlarmApp ownership marker. Nothing will be overwritten.' }
        $decoded = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String(($readme -join '')))
        if (-not $decoded.Contains('<!-- alarmapp-download-repository -->')) { throw 'The existing repository is not marked as this download repository. Nothing will be overwritten.' }
    }
    $existingRelease = & $cli.Source release view v1.0.0 --repo $full 2>$null
    if ($LASTEXITCODE -eq 0) { throw 'Release v1.0.0 already exists. It will not be overwritten.' }
    if ($createdRepository) {
    $readmeText = @'
<!-- alarmapp-download-repository -->
# AlarmApp for Windows

Scheduled alarms, finite interval reminders, WAV/MP3 sounds, English/Simplified Chinese, monthly usage summaries and daily records.

[Download the latest release](../../releases/latest)

- Portable ZIP: extract all files and run AlarmApp.exe.
- Setup ZIP: extract all files and run Install.cmd.
- Requires Windows desktop and .NET Framework 4.8 or later.
- Runs independently of Codex. No account or internet connection is needed after download.
- The setup package is a current-user script-based installer with shortcuts and uninstall support.
- Releases are unsigned. See the release notes for verification scope.

This repository distributes binary packages. Source reuse licensing has not been selected.
'@
    $readmeText = $readmeText.Replace('[Download the latest release](../../releases/latest)',('[Download the latest release](https://github.com/'+$full+'/releases/latest)'))
    $sha = & $cli.Source api ('repos/'+$full+'/contents/README.md') --jq .sha
    if ($LASTEXITCODE -ne 0) { throw 'Could not read the download README version.' }
    $content = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($readmeText))
    $body = @{message='Document AlarmApp downloads';content=$content;sha=($sha -join '').Trim()} | ConvertTo-Json
    $request = Join-Path $artifacts 'publish-readme-request.json'
    [IO.File]::WriteAllText($request,$body,(New-Object Text.UTF8Encoding($false)))
    & $cli.Source api ('repos/'+$full+'/contents/README.md') --method PUT --input $request 1>$null
    if ($LASTEXITCODE -ne 0) { throw 'Could not update the download README.' }
    }
    & $cli.Source release create v1.0.0 $portable $setup $checksums --repo $full --title 'AlarmApp 1.0.0 for Windows' --notes-file (Join-Path $repo 'docs/GITHUB_RELEASE.md')
    if ($LASTEXITCODE -ne 0) { throw 'Release publication failed.' }
    & $cli.Source release view v1.0.0 --repo $full --json url --jq .url
    if ($LASTEXITCODE -ne 0) { throw 'Release was submitted but its URL could not be verified.' }
} catch {
    Write-Host ('Publish failed: '+$_.Exception.Message) -ForegroundColor Red
    exit 1
}
