@echo off
title BarnaMu Web AutoRestart
:loop
echo [%date% %time%] Iniciando BarnaMu Web...
cd /d C:\MuDev\BarnaMuWeb
dotnet run --configuration Release
echo [%date% %time%] Web caida - Reiniciando en 10 segundos...
timeout /t 10
goto loop
