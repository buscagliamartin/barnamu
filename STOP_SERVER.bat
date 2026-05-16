@echo off
echo ============================================
echo    BarnaMu - Deteniendo Servidor
echo ============================================
taskkill /F /FI "WINDOWTITLE eq BarnaMu AutoRestart*" /T
wmic process where "CommandLine like '%%OpenMU%%' and Name='dotnet.exe'" call terminate >nul 2>&1
echo [OK] Servidor detenido.
timeout /t 3
