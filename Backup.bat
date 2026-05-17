@echo off
setlocal

REM ============================================================================
REM BarnaMu — Backup.bat
REM Daily backup (always overwrites previous day's daily file).
REM On Sundays, also rolls over to a separate weekly file (overwrites previous week's).
REM No auto-push: Push.bat calls this script first, then commits the resulting files.
REM ============================================================================

set BACKUP_DIR=C:\MuDev\Backups
set PGPASSWORD=W3l.c0m3
set PG_DUMP="C:\Program Files\PostgreSQL\16\bin\pg_dump.exe"

if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

echo [%DATE% %TIME%] Iniciando backup diario...

REM 1) Daily backup (always overwrites)
%PG_DUMP% -U postgres -d openmu -f "%BACKUP_DIR%\barnamu_daily.sql"
if %ERRORLEVEL% NEQ 0 (
    echo [%DATE% %TIME%] [ERROR] pg_dump fallo con codigo %ERRORLEVEL%
    exit /b 1
)
echo [%DATE% %TIME%] Backup diario OK: %BACKUP_DIR%\barnamu_daily.sql

REM 2) Weekly backup on Sundays (DayOfWeek = 0)
for /f %%i in ('powershell -NoProfile -Command "(Get-Date).DayOfWeek.value__"') do set DOW=%%i
if "%DOW%"=="0" (
    copy /Y "%BACKUP_DIR%\barnamu_daily.sql" "%BACKUP_DIR%\barnamu_weekly.sql" >nul
    echo [%DATE% %TIME%] Backup semanal actualizado: %BACKUP_DIR%\barnamu_weekly.sql
)

REM 3) Cleanup legacy timestamped backups (barnamu_2YYYY-MM-DD_*.sql + barnamu_latest.sql).
REM    NO toca barnamu_daily.sql ni barnamu_weekly.sql (no empiezan con "2" o "l").
forfiles /p "%BACKUP_DIR%" /m "barnamu_2*.sql" /c "cmd /c del @path" 2>nul
if exist "%BACKUP_DIR%\barnamu_latest.sql" del "%BACKUP_DIR%\barnamu_latest.sql"

echo [%DATE% %TIME%] Backup completado.
endlocal
