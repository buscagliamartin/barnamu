@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

if not defined BARNAMU_HOME set "BARNAMU_HOME=%SCRIPT_DIR%"
if not exist "%BARNAMU_HOME%\OpenMU\src" if exist "C:\MuDev\OpenMU\src" set "BARNAMU_HOME=C:\MuDev"
if "%BARNAMU_HOME:~-1%"=="\" set "BARNAMU_HOME=%BARNAMU_HOME:~0,-1%"

set "ROOT=%BARNAMU_HOME%"
set "OPENMU_SRC=%BARNAMU_HOME%\OpenMU\src"
set "STARTUP_DIR=%OPENMU_SRC%\Startup"
set "WEB_PUBLISH=%BARNAMU_HOME%\BarnaMuWeb\publish"
set "BACKUP_DIR=%BARNAMU_HOME%\Backups"

if not defined BARNAMU_PG_DUMP set "BARNAMU_PG_DUMP=C:\Program Files\PostgreSQL\16\bin\pg_dump.exe"
if not defined BARNAMU_DB_NAME set "BARNAMU_DB_NAME=openmu"
if not defined BARNAMU_DB_USER set "BARNAMU_DB_USER=postgres"
if not defined BARNAMU_SERVER_HOST set "BARNAMU_SERVER_HOST=barnamu.ddns.net"
if not defined BARNAMU_WEB_PORT set "BARNAMU_WEB_PORT=8081"
if not defined BARNAMU_WEB_URL set "BARNAMU_WEB_URL=http://*:8081"

set "PG_DUMP=%BARNAMU_PG_DUMP%"
set "DB_NAME=%BARNAMU_DB_NAME%"
set "DB_USER=%BARNAMU_DB_USER%"
set "SERVER_HOST=%BARNAMU_SERVER_HOST%"
set "WEB_PORT=%BARNAMU_WEB_PORT%"
set "WEB_URL=%BARNAMU_WEB_URL%"

if not defined BARNAMU_DB_PASSWORD set "BARNAMU_DB_PASSWORD=W3l.c0m3"
set "DOTNET_NOLOGO=1"
set "DOTNET_CLI_UI_LANGUAGE=en"
set "MSBUILDDISABLENODEREUSE=1"

if "%~1"=="" goto :help

set "CMD=%~1"
if /I "%CMD%"=="help" goto :help
if /I "%CMD%"=="build" goto :build
if /I "%CMD%"=="rebuild" goto :build
if /I "%CMD%"=="start" goto :start_dispatch
if /I "%CMD%"=="server" goto :start_server
if /I "%CMD%"=="web" goto :start_web
if /I "%CMD%"=="all" goto :start_all
if /I "%CMD%"=="run-server" goto :run_server
if /I "%CMD%"=="run-web" goto :run_web
if /I "%CMD%"=="stop" goto :stop_dispatch
if /I "%CMD%"=="backup" goto :backup
if /I "%CMD%"=="push" goto :push
if /I "%CMD%"=="status" goto :status

echo [error] Unknown command: %CMD%
echo.
goto :help

:help
echo BarnaMu control script
echo.
echo Usage:
echo   BarnaMu.bat build
echo   BarnaMu.bat start server
echo   BarnaMu.bat start web
echo   BarnaMu.bat start all
echo   BarnaMu.bat stop server
echo   BarnaMu.bat stop web
echo   BarnaMu.bat stop all
echo   BarnaMu.bat backup
echo   BarnaMu.bat push "commit message"
echo   BarnaMu.bat status
echo.
exit /b 0

:build
echo [build] Stopping OpenMU processes...
call :stop_server_quiet
taskkill /F /IM msbuild.exe >nul 2>nul

call :require_dir "%OPENMU_SRC%" "OpenMU source directory" || exit /b 1
call :require_dir "%STARTUP_DIR%" "OpenMU Startup directory" || exit /b 1

echo [build] Cleaning Startup bin/obj...
rd /S /Q "%STARTUP_DIR%\bin" >nul 2>nul
rd /S /Q "%STARTUP_DIR%\obj" >nul 2>nul

echo [build] Building OpenMU Release...
pushd "%OPENMU_SRC%" >nul
dotnet build "MUnique.OpenMU.sln" --configuration Release --nologo -v:q -p:RunAnalyzers=false -p:WarningLevel=0 -clp:ErrorsOnly;Summary
set "RC=%ERRORLEVEL%"
popd >nul

if not "%RC%"=="0" (
    echo [error] Build failed with code %RC%.
    exit /b %RC%
)

echo [ok] Build completed.
exit /b 0

:start_dispatch
if /I "%~2"=="server" goto :start_server
if /I "%~2"=="web" goto :start_web
if /I "%~2"=="all" goto :start_all
echo [error] Use: BarnaMu.bat start server^|web^|all
exit /b 1

:start_all
set "ALL_RC=0"
call :start_web
if errorlevel 1 set "ALL_RC=1"
call :start_server
if errorlevel 1 set "ALL_RC=1"
exit /b %ALL_RC%

:start_server
call :require_dir "%STARTUP_DIR%" "OpenMU Startup directory" || exit /b 1
echo [server] Starting auto-restart window...
call :stop_server_quiet
start "BarnaMu Server" cmd /k ""%~f0" run-server"
echo [ok] Server window started.
exit /b 0

:start_web
call :require_dir "%WEB_PUBLISH%" "BarnaMuWeb publish directory" || exit /b 1
if not exist "%WEB_PUBLISH%\BarnaMuWeb.exe" (
    echo [error] Missing "%WEB_PUBLISH%\BarnaMuWeb.exe".
    exit /b 1
)
echo [web] Starting auto-restart window...
call :stop_web_quiet
call :require_free_port "%WEB_PORT%" "web"
if errorlevel 1 exit /b 1
start "BarnaMu Web" cmd /k ""%~f0" run-web"
echo [ok] Web window started.
exit /b 0

:run_server
title BarnaMu Server
call :require_dir "%STARTUP_DIR%" "OpenMU Startup directory" || exit /b 1

:server_loop
echo [server] Starting OpenMU...
pushd "%STARTUP_DIR%" >nul
set "DB_ADMIN_PW=%BARNAMU_DB_PASSWORD%"
dotnet run --configuration Release --no-build --no-restore -- -resolveIP:%SERVER_HOST%
set "RC=%ERRORLEVEL%"
popd >nul
echo [server] Stopped with code %RC%. Restarting in 10 seconds. Press Ctrl+C to stop.
timeout /T 10 /NOBREAK >nul
goto :server_loop

:run_web
title BarnaMu Web
call :require_dir "%WEB_PUBLISH%" "BarnaMuWeb publish directory" || exit /b 1
if not exist "%WEB_PUBLISH%\BarnaMuWeb.exe" (
    echo [error] Missing "%WEB_PUBLISH%\BarnaMuWeb.exe".
    exit /b 1
)

:web_loop
call :require_free_port "%WEB_PORT%" "web"
if errorlevel 1 exit /b 1
echo [web] Starting BarnaMuWeb on %WEB_URL%...
pushd "%WEB_PUBLISH%" >nul
set "ASPNETCORE_URLS=%WEB_URL%"
BarnaMuWeb.exe
set "RC=%ERRORLEVEL%"
popd >nul
echo [web] Stopped with code %RC%. Restarting in 10 seconds. Press Ctrl+C to stop.
timeout /T 10 /NOBREAK >nul
goto :web_loop

:stop_dispatch
if /I "%~2"=="server" goto :stop_server
if /I "%~2"=="web" goto :stop_web
if /I "%~2"=="all" goto :stop_all
echo [error] Use: BarnaMu.bat stop server^|web^|all
exit /b 1

:stop_all
echo [stop] Stopping server and web...
call :stop_server_quiet
call :stop_web_quiet
echo [ok] Server and web stopped.
exit /b 0

:stop_server
echo [stop] Stopping server...
call :stop_server_quiet
echo [ok] Server stopped.
exit /b 0

:stop_web
echo [stop] Stopping web...
call :stop_web_quiet
echo [ok] Web stopped.
exit /b 0

:stop_server_quiet
taskkill /F /FI "WINDOWTITLE eq BarnaMu Server*" /T >nul 2>nul
call :kill_by_command "OpenMU"
exit /b 0

:stop_web_quiet
taskkill /F /FI "WINDOWTITLE eq BarnaMu Web*" /T >nul 2>nul
call :kill_by_command "BarnaMuWeb"
call :kill_by_port "%WEB_PORT%"
exit /b 0

:backup
call :require_file "%PG_DUMP%" "pg_dump executable" || exit /b 1
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%" >nul 2>nul

set "PGPASSWORD=%BARNAMU_DB_PASSWORD%"
echo [backup] Writing daily database backup...
"%PG_DUMP%" --no-password -U "%DB_USER%" -d "%DB_NAME%" -f "%BACKUP_DIR%\barnamu_daily.sql" >nul
set "RC=%ERRORLEVEL%"
if not "%RC%"=="0" (
    echo [error] pg_dump failed with code %RC%.
    exit /b %RC%
)

for /F %%i in ('powershell -NoProfile -Command "(Get-Date).DayOfWeek.value__"') do set "DOW=%%i"
if "%DOW%"=="0" (
    copy /Y "%BACKUP_DIR%\barnamu_daily.sql" "%BACKUP_DIR%\barnamu_weekly.sql" >nul
    echo [backup] Weekly backup refreshed.
)

forfiles /P "%BACKUP_DIR%" /M "barnamu_2*.sql" /C "cmd /c del @path" >nul 2>nul
if exist "%BACKUP_DIR%\barnamu_latest.sql" del "%BACKUP_DIR%\barnamu_latest.sql" >nul 2>nul

echo [ok] Backup completed: %BACKUP_DIR%\barnamu_daily.sql
exit /b 0

:push
set "MSG=%~2"
if "%MSG%"=="" set /P "MSG=Commit message: "
if "%MSG%"=="" (
    echo [error] Commit message is required.
    exit /b 1
)

call :backup
if errorlevel 1 exit /b 1

git -C "%ROOT%" rev-parse --is-inside-work-tree >nul 2>nul
if errorlevel 1 (
    echo [error] Not a git repository: %ROOT%
    exit /b 1
)

echo [git] Preparing commit...
pushd "%ROOT%" >nul
git add -A >nul
if errorlevel 1 (
    popd >nul
    echo [error] git add failed.
    exit /b 1
)

git diff --cached --quiet >nul 2>nul
set "DIFF=%ERRORLEVEL%"
if "%DIFF%"=="0" (
    popd >nul
    echo [ok] No new changes to push.
    exit /b 0
)
if not "%DIFF%"=="1" (
    popd >nul
    echo [error] git diff failed.
    exit /b %DIFF%
)

echo [git] Staged changes:
git status --short

echo [git] Committing...
git commit -m "%MSG%" --quiet
if errorlevel 1 (
    popd >nul
    echo [error] git commit failed.
    exit /b 1
)

echo [git] Pushing...
git push --quiet
set "RC=%ERRORLEVEL%"
popd >nul
if not "%RC%"=="0" (
    echo [error] git push failed with code %RC%.
    exit /b %RC%
)

echo [ok] Push completed.
exit /b 0

:status
echo [status] Matching BarnaMu processes:
powershell -NoProfile -ExecutionPolicy Bypass -Command "$self=$PID; Get-CimInstance Win32_Process | Where-Object { $_.ProcessId -ne $self -and $_.CommandLine -and ($_.CommandLine -like '*OpenMU*' -or $_.CommandLine -like '*BarnaMuWeb*') } | Select-Object ProcessId,Name,CommandLine | Format-Table -AutoSize"
echo [status] Web port %WEB_PORT%:
call :show_port_owner "%WEB_PORT%"
exit /b 0

:require_dir
if not exist "%~1\" (
    echo [error] Missing %~2: %~1
    exit /b 1
)
exit /b 0

:require_file
if not exist "%~1" (
    echo [error] Missing %~2: %~1
    exit /b 1
)
exit /b 0

:kill_by_command
powershell -NoProfile -ExecutionPolicy Bypass -Command "$self=$PID; $pattern='%~1'; Get-CimInstance Win32_Process | Where-Object { $_.ProcessId -ne $self -and $_.CommandLine -and $_.CommandLine -like ('*' + $pattern + '*') } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }" >nul 2>nul
exit /b 0

:kill_by_port
powershell -NoProfile -ExecutionPolicy Bypass -Command "$port=[int]'%~1'; Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique | ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }" >nul 2>nul
exit /b 0

:require_free_port
powershell -NoProfile -ExecutionPolicy Bypass -Command "$port=[int]'%~1'; $name='%~2'; $conn=Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1; if ($conn) { $proc=Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue; if ($proc) { Write-Host ('[error] Port ' + $port + ' for ' + $name + ' is already in use by PID ' + $proc.Id + ' (' + $proc.ProcessName + ').'); } else { Write-Host ('[error] Port ' + $port + ' for ' + $name + ' is already in use by PID ' + $conn.OwningProcess + '.'); }; exit 1 }; exit 0"
exit /b %ERRORLEVEL%

:show_port_owner
powershell -NoProfile -ExecutionPolicy Bypass -Command "$port=[int]'%~1'; $conns=Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue; if (-not $conns) { Write-Host ('[ok] Port ' + $port + ' is free.'); exit 0 }; $conns | Select-Object -ExpandProperty OwningProcess -Unique | ForEach-Object { $proc=Get-Process -Id $_ -ErrorAction SilentlyContinue; if ($proc) { Write-Host ('[info] Port ' + $port + ' is used by PID ' + $proc.Id + ' (' + $proc.ProcessName + ').'); } else { Write-Host ('[info] Port ' + $port + ' is used by PID ' + $_ + '.'); } }"
exit /b 0
