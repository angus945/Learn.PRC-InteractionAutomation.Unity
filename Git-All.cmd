@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Git-All.ps1"
set "exitCode=%ERRORLEVEL%"
endlocal & exit /b %exitCode%
