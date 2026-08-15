@echo off
rem Purity check: core/src must contain no float/double/System.Random/DateTime/UnityEngine.
rem Enforces CLAUDE.md hard rules 1 and 2 mechanically. Run from anywhere.
setlocal
cd /d "%~dp0.."

set FAIL=0
for %%W in (float double System.Random DateTime UnityEngine) do (
    findstr /s /n /r "\<%%W\>" core\src\*.cs >nul 2>&1
    if not errorlevel 1 (
        echo PURITY VIOLATION: forbidden token '%%W' found in core\src:
        findstr /s /n /r "\<%%W\>" core\src\*.cs
        set FAIL=1
    )
)

if "%FAIL%"=="1" exit /b 1
echo Purity check passed: core\src is free of float/double/System.Random/DateTime/UnityEngine.
exit /b 0
