@echo off
title Recompilar BarnaMu
echo [%date% %time%] Cerrando procesos para liberar DLLs...
taskkill /f /im MUnique.OpenMU.Startup.exe /im dotnet.exe /im msbuild.exe 2>nul
timeout /t 2 /nobreak >nul

cd /d C:\MuDev\OpenMU\src\Startup
rd /s /q bin obj 2>nul
dotnet build --configuration Release
pause