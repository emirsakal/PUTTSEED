@echo off
rem Full core check: purity grep + dotnet test. Run from anywhere.
setlocal
cd /d "%~dp0.."

call scripts\check-purity.bat
if errorlevel 1 exit /b 1

dotnet test core -c Release --nologo
if errorlevel 1 exit /b 1

exit /b 0
