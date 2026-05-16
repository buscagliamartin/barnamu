@echo off
setlocal

REM ============================================================================
REM BarnaMu — Push.bat
REM Quick helper to push C:\MuDev changes to GitHub.
REM Usage:  Push.bat "Brief commit message"
REM         Push.bat                      (will prompt for message)
REM ============================================================================

set "MSG=%~1"

if "%MSG%"=="" (
    set /p "MSG=Mensaje de commit: "
)

if "%MSG%"=="" (
    echo [ERROR] Necesitas un mensaje de commit.
    exit /b 1
)

echo.
echo ============================================
echo     BarnaMu - Push a GitHub
echo ============================================
echo Mensaje: %MSG%
echo.

cd /d C:\MuDev

git add -A
if %ERRORLEVEL% NEQ 0 goto :error

git diff --cached --quiet
if %ERRORLEVEL% EQU 0 (
    echo [INFO] No hay cambios nuevos para pushear.
    exit /b 0
)

echo Cambios:
git status --short
echo.

git commit -m "%MSG%"
if %ERRORLEVEL% NEQ 0 goto :error

git push
if %ERRORLEVEL% NEQ 0 goto :error

echo.
echo ============================================
echo     Push exitoso a GitHub
echo ============================================
exit /b 0

:error
echo.
echo [ERROR] Fallo el push. Revisa los errores arriba.
exit /b 1
