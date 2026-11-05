@echo off
setlocal
set BINARIES_DIRECTORY=%~dp0

set "BINARIES_DIRECTORY=" && "%BINARIES_DIRECTORY%as.exe" @gas-arch=i686-linux-android-as %*
if not ERRORLEVEL 0 exit /b %errorlevel%
