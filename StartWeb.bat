@echo off
title BarnaMu Web AutoRestart
:: Definimos que .NET escuche en el puerto 8081 para todas las IPs
set ASPNETCORE_URLS=http://*:8081

:loop
echo [%date% %time%] Iniciando BarnaMu Web (Production)...
:: Nos paramos en la carpeta del publish que creaste en el paso 4
cd /d "C:\MuDev\BarnaMuWeb\publish"
:: Ejecutamos el binario directo en vez de usar 'dotnet run'
BarnaMuWeb.exe
echo [%date% %time%] Web caida - Reiniciando en 10 segundos...
timeout /t 10
goto loop