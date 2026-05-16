@echo off
title BarnaMu AutoRestart

REM Iniciar la web en una ventana aparte solo al arrancar el script
echo [%date% %time%] Lanzando BarnaMu Web...
start "BarnaMu Web" cmd /c StartWeb.bat

:loop
echo [%date% %time%] Iniciando BarnaMu Server...
cd /d C:\MuDev\OpenMU\src\Startup
set DB_ADMIN_PW=W3l.c0m3
dotnet run --configuration Release -- -resolveIP:barnamu.ddns.net
echo [%date% %time%] Servidor caido - Reiniciando en 10 segundos...
timeout /t 10
goto loop