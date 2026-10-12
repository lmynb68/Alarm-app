@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0VerifyAndroid.ps1"
set "buildResult=%errorlevel%"
echo.
pause
exit /b %buildResult%
