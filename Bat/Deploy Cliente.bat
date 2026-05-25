@echo off
setlocal enabledelayedexpansion

set "MUMAIN_ROOT=C:\MuDev\MuMain"
set "ORIGEN_VM=%MUMAIN_ROOT%\out\build\windows-x86\src\Release"
set "DESTINO_LOCAL=\\vmware-host\Shared Folders\Archivos\MuMain\Release"

echo ========================================================
echo [1/2] Building MuMain client...
echo ========================================================

pushd "%MUMAIN_ROOT%" >nul
cmake --build --preset windows-x86-release
set "BUILD_RC=%ERRORLEVEL%"
popd >nul

if not "%BUILD_RC%"=="0" (
    echo.
    echo Build failed. Process stopped.
    pause
    exit /b %BUILD_RC%
)

echo.
echo ========================================================
echo [2/2] Copying build output to local release folder...
echo ========================================================

rem Copy the whole Release build output (Main.exe + MUnique.Client.Library.dll + any
rem other binaries) so the client and its managed library never get out of sync.
rem /E = include subfolders ; no /MIR, so existing game data in the destination is kept.
robocopy "%ORIGEN_VM%" "%DESTINO_LOCAL%" /E /NFL /NDL /R:3 /W:5
rem robocopy exit codes 0-7 mean success; 8 or higher means a real failure.
if errorlevel 8 (
    echo.
    echo Copy failed.
    pause
    exit /b 1
)

echo.
echo ========================================================
echo Client ready for local testing.
echo ========================================================
pause
exit /b 0