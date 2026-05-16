@echo off
echo ============================================
echo    BarnaMu - Deteniendo Web
echo ============================================
taskkill /F /FI "WINDOWTITLE eq BarnaMu Web AutoRestart*" /T
wmic process where "CommandLine like '%%BarnaMuWeb%%' and Name='dotnet.exe'" call terminate >nul 2>&1
echo [OK] Web detenida.
timeout /t 3
