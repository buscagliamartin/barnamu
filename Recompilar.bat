@echo off
title Recompilar BarnaMu
echo [%date% %time%] Cerrando procesos para liberar DLLs...
taskkill /f /im MUnique.OpenMU.Startup.exe /im dotnet.exe /im msbuild.exe 2>nul
timeout /t 2 /nobreak >nul

cd /d C:\MuDev\OpenMU\src\Startup
rd /s /q bin obj 2>nul

echo [%date% %time%] Compilando... (Ocultando warnings molestos)
:: Usamos -v m (Minimal) pero apagamos los analizadores y warnings por completo
dotnet build --configuration Release -p:RunAnalyzers=false -p:WarningLevel=0 -v m

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ==========================================
    echo  ¡COMPILACION COMPLETADA CON EXITO! 🚀
    echo ==========================================
) else (
    echo.
    echo ==========================================
    echo  ❌ ERROR: La compilacion fallo.
    echo ==========================================
)

echo.
pause