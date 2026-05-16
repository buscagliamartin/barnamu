@echo off
echo ============================================
echo    BarnaMu - Deteniendo TODO
echo ============================================
taskkill /F /FI "WINDOWTITLE eq BarnaMu AutoRestart*" /T
taskkill /F /FI "WINDOWTITLE eq BarnaMu Web AutoRestart*" /T
wmic process where "CommandLine like '%%OpenMU%%' and Name='dotnet.exe'" call terminate >nul 2>&1
wmic process where "CommandLine like '%%BarnaMuWeb%%' and Name='dotnet.exe'" call terminate >nul 2>&1
echo [OK] Servidor y Web detenidos.
timeout /t 3
