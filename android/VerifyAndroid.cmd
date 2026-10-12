@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0VerifyAndroid.ps1"
echo.
pause
