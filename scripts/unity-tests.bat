@echo off
rem Unity EditMode tests in batch mode. Run from anywhere.
setlocal
cd /d "%~dp0.."

rem Override with a UNITY_EXE env var when your editor lives elsewhere.
if not defined UNITY_EXE set UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe
if not exist "%UNITY_EXE%" (
    echo Unity editor not found at %UNITY_EXE% - set the UNITY_EXE env var.
    exit /b 1
)

if not exist artifacts mkdir artifacts

"%UNITY_EXE%" -batchmode -projectPath "%CD%" -runTests -testPlatform EditMode ^
    -testResults "%CD%\artifacts\editmode-results.xml" ^
    -logFile "%CD%\artifacts\unity-tests.log"
set RESULT=%ERRORLEVEL%

if %RESULT% NEQ 0 (
    echo EditMode tests FAILED ^(exit %RESULT%^). See artifacts\editmode-results.xml and artifacts\unity-tests.log
    exit /b %RESULT%
)

echo EditMode tests passed.
exit /b 0
