@echo off
call "%~dp0BarnaMu.bat" start web
call "%~dp0BarnaMu.bat" run-server
exit /b %ERRORLEVEL%
