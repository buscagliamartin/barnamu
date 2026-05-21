@echo off
setlocal enabledelayedexpansion

:: Configurar Rutas
set "ORIGEN_VM=C:\MuDev\MuMain\out\build\windows-x86\src\Release"
set "DESTINO_LOCAL=\\vmware-host\Shared Folders\Archivos\MuMain\Release"

echo ========================================================
echo [1/3] Compilando el proyecto...
echo ========================================================
cmake --build --preset windows-x86-release

:: Verificamos si la compilación tuvo éxito (errorlevel 0 significa éxito)
if %errorlevel% neq 0 (
    echo.
    echo ¡Error en la compilación! El proceso se detendrá aquí.
    pause
    exit /b %errorlevel%
)

echo.
echo ========================================================
echo [2/3] Copiando ejecutables compilados a Maquina Local...
echo ========================================================
copy /Y "C:\MuDev\MuMain\out\build\windows-x86\src\Release\Main.exe" "\\vmware-host\Shared Folders\Archivos\MuMain\Release\Main.exe"

:: :: echo.
:: :: echo [3/3] Sincronizando carpeta Data del Cliente...
:: :: robocopy "%DATA_LOCAL%" "%DESTINO_LOCAL%\Data" /E /IS /NDL /NFL /R:3 /W:5

echo.
echo ========================================================
echo Proceso finalizado. ¡Cliente listo para testear en Local!
echo ========================================================
pause