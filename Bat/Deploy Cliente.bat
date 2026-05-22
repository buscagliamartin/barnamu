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
echo [2/2] Copying compiled client to local release folder...
echo ========================================================

copy /Y "%ORIGEN_VM%\Main.exe" "%DESTINO_LOCAL%\Main.exe"
if errorlevel 1 (
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