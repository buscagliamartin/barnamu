@echo off
echo ============================================
echo     BarnaMu - Recompilando servidor...
echo ============================================
echo.

cd C:\MuDev\OpenMU\src

dotnet build MUnique.OpenMU.sln --configuration Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] La compilacion fallo. Revisa los errores arriba.
    pause
    exit /b 1
)

echo.
echo ============================================
echo     Compilacion exitosa!
echo ============================================
echo.
pause
