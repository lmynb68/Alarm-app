param([string]$StudioHome='D:/Android Studio',[string]$SdkHome=(Join-Path $env:LOCALAPPDATA 'Android/Sdk'))
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath($PSScriptRoot)
$localSdk=Join-Path $projectRoot '.toolchain/android-sdk'
if(-not $PSBoundParameters.ContainsKey('SdkHome') -and (Test-Path -LiteralPath $localSdk)){$SdkHome=$localSdk}
$compilerHome=Join-Path $StudioHome 'jbr'
$localJdk=Join-Path $projectRoot '.toolchain/jdk-17'
if(Test-Path -LiteralPath (Join-Path $localJdk 'bin/javac.exe')){$compilerHome=$localJdk}
$reportDir=Join-Path $projectRoot 'verification'
New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
$log=Join-Path $reportDir 'verify.log'
$resultFile=Join-Path $reportDir 'result.json'
$result=@{ status='running'; started=(Get-Date).ToString('o'); sourceCompile=$false; unitTests=$false; apkBuild=$false; lint=$false; deviceTest=$false }
$transcript=$false
function Save-Result {
 [IO.File]::WriteAllText($resultFile,($result | ConvertTo-Json -Depth 6),(New-Object Text.UTF8Encoding($false)))
}
function Run-Native([string]$program,[string[]]$arguments) {
 $result.stage=[IO.Path]::GetFileName($program)
 Save-Result
 Write-Host ('Running: '+$result.stage+'. Please keep this window open until the final result.')
 $nativeLog=Join-Path $reportDir 'native-build.log'
 $previousPreference=$ErrorActionPreference
 try {
  $ErrorActionPreference='Continue'
  & $program @arguments 2>&1 | ForEach-Object {
   $line=$_.ToString()
   [IO.File]::AppendAllText($nativeLog,$line+[Environment]::NewLine,(New-Object Text.UTF8Encoding($false)))
   Write-Host $line
  }
  $exitCode=$LASTEXITCODE
 } finally {$ErrorActionPreference=$previousPreference}
 if ($exitCode -ne 0) { throw ('Command failed with exit code '+$exitCode+': '+$program) }
}

try {
 Start-Transcript -Path $log -Force | Out-Null
 $transcript=$true
 Save-Result
 [IO.File]::WriteAllText((Join-Path $reportDir 'native-build.log'),'',(New-Object Text.UTF8Encoding($false)))
 $candidates=@()
 if(Test-Path -LiteralPath $localJdk){$candidates+=$localJdk}
 if($env:JAVA_HOME){$candidates+=$env:JAVA_HOME}
 $jdkDir=Join-Path $env:USERPROFILE '.jdks'
 if(Test-Path -LiteralPath $jdkDir){$candidates+=@(Get-ChildItem -LiteralPath $jdkDir -Directory | ForEach-Object {$_.FullName})}
 $buildJdk=$null
 foreach($candidate in $candidates){$releaseFile=Join-Path $candidate 'release';if(Test-Path -LiteralPath $releaseFile){$releaseText=Get-Content -LiteralPath $releaseFile -Raw;if($releaseText -match 'JAVA_VERSION="(17|21)[.]' -and (Test-Path -LiteralPath (Join-Path $candidate 'bin/java.exe'))){$buildJdk=$candidate;break}}}
 if(-not $buildJdk){$result.status='source-checks-only';$result.next='Install/select JDK 17 in Android Studio Gradle settings, then run this script again.';Write-Host $result.next -ForegroundColor Yellow}
 elseif(-not(Test-Path -LiteralPath (Join-Path $SdkHome 'platforms/android-35/android.jar')) -or -not(Test-Path -LiteralPath (Join-Path $SdkHome 'build-tools/35.0.0/aapt2.exe'))){$result.status='source-checks-only';$result.next='Install Android SDK Platform 35 and Build Tools 35.0.0 using SDK Manager, then rerun.';Write-Host $result.next -ForegroundColor Yellow}
 else {
  $env:JAVA_HOME=$buildJdk
  $env:ANDROID_HOME=$SdkHome
  $env:ANDROID_SDK_ROOT=$SdkHome
  $env:GRADLE_USER_HOME=Join-Path $projectRoot '.gradle-user'
  $result.gradleJdk=$buildJdk
  $sourceSnapshot=@(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'app/src') -Recurse -File | Sort-Object FullName | ForEach-Object {$_.FullName+' '+(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash})
  Push-Location $projectRoot
  try{Run-Native (Join-Path $projectRoot 'gradlew.bat') @('-Duser.language=en','-Duser.country=US','--no-daemon','--console=plain','--stacktrace','testDebugUnitTest','lintDebug','assembleDebug')}finally{Pop-Location}
  $afterSnapshot=@(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'app/src') -Recurse -File | Sort-Object FullName | ForEach-Object {$_.FullName+' '+(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash})
  if(($sourceSnapshot -join '|') -ne ($afterSnapshot -join '|')){throw 'Sources changed during the build. Run the build again.'}
  $apk=Join-Path $projectRoot 'app/build/outputs/apk/debug/app-debug.apk'
  if(-not(Test-Path -LiteralPath $apk)){throw 'Build ended without an APK.'}
  $result.sourceCompile=$true;$result.apkBuild=$true;$result.lint=$true
  $testDir=Join-Path $projectRoot 'app/build/test-results/testDebugUnitTest'
  $testFiles=@(Get-ChildItem -LiteralPath $testDir -Filter 'TEST-*.xml')
  if($testFiles.Count -eq 0){throw 'No Gradle unit test results found.'}
  $testCount=0
  foreach($testFile in $testFiles){[xml]$testXml=Get-Content -LiteralPath $testFile.FullName -Raw -Encoding UTF8;if([int]$testXml.testsuite.failures -gt 0 -or [int]$testXml.testsuite.errors -gt 0){throw 'Unit test report contains failures.'};$testCount += [int]$testXml.testsuite.tests}
  if($testCount -eq 0){throw 'No unit tests executed.'}
  $result.unitTests=$true;$result.testsPassed=$testCount;$result.status='build-passed-device-validation-pending'
  $deliveryDir=[IO.Path]::GetFullPath((Join-Path $projectRoot '../../AlarmApp-Android-Release'))
  New-Item -ItemType Directory -Path $deliveryDir -Force | Out-Null
  $deliveryApk=Join-Path $deliveryDir 'AlarmApp-Android-1.0.0-Debug.apk'
  Copy-Item -LiteralPath $apk -Destination $deliveryApk -Force
  $result.deliveryApk=$deliveryApk
  $checksum=(Get-FileHash -LiteralPath $deliveryApk -Algorithm SHA256).Hash.ToLowerInvariant()
  [IO.File]::WriteAllText((Join-Path $deliveryDir 'SHA256SUMS.txt'),$checksum+'  AlarmApp-Android-1.0.0-Debug.apk'+[Environment]::NewLine,(New-Object Text.UTF8Encoding($false)))
  $sourceHashes=@(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'app/src') -Recurse -File | Sort-Object FullName | ForEach-Object {@{path=$_.FullName.Substring($projectRoot.Length+1);sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
  $result.sources=$sourceHashes
  $result.apk=$apk;$result.apkSHA256=(Get-FileHash -LiteralPath $apk -Algorithm SHA256).Hash
  Write-Host 'APK, Gradle tests and lint passed. Device installation and real ringing are not yet verified.' -ForegroundColor Green
 }
} catch {$result.status='failed';$result.error=$_.Exception.Message;Write-Host ('Verification failed: '+$result.error) -ForegroundColor Red}
finally {
 $result.finished=(Get-Date).ToString('o')
 $result.stage='finished'
 [IO.File]::WriteAllText($resultFile,($result | ConvertTo-Json -Depth 6),(New-Object Text.UTF8Encoding($false)))
 if($transcript){Stop-Transcript | Out-Null}
 Write-Host ('Results: '+$resultFile)
 Write-Host ('Log: '+$log)
}
if($result.status -eq 'failed'){exit 1}
