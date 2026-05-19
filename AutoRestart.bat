@echo off
title BarnaMu AutoRestart

echo [%date% %time%] Lanzando BarnaMu Web...
start "BarnaMu Web" cmd /c StartWeb.bat

:loop
echo [%date% %time%] Limpiando instancias colgadas antes de encender...
taskkill /f /im MUnique.OpenMU.Startup.exe /im dotnet.exe /im msbuild.exe 2>nul
timeout /t 2 /nobreak >nul

echo [%date% %time%] Iniciando BarnaMu Server...
cd /d C:\MuDev\OpenMU\src\Startup
set DB_ADMIN_PW=W3l.c0m3
dotnet run --configuration Release -- -resolveIP:barnamu.ddns.net

