@echo off
set BACKUP_DIR=C:\MuDev\Backups
set PGPASSWORD=W3l.c0m3

if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

for /f "tokens=*" %%i in ('powershell -Command "Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'"') do set TIMESTAMP=%%i

set FILENAME=%BACKUP_DIR%\barnamu_%TIMESTAMP%.sql

"C:\Program Files\PostgreSQL\16\bin\pg_dump.exe" -U postgres -d openmu -f "%FILENAME%"

copy /y "%FILENAME%" "%BACKUP_DIR%\barnamu_latest.sql"

echo Backup completado: %FILENAME%
echo Actualizado: %BACKUP_DIR%\barnamu_latest.sql

forfiles /p "%BACKUP_DIR%" /s /m *.sql /d -7 /c "cmd /c del @path" 2>nul
