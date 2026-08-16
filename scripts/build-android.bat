@echo off
rem Batch-mode Android build. Default output: artifacts\PuttSeed.aab.
rem Pass "apk" as the first argument for an installable artifacts\PuttSeed.apk.
setlocal
cd /d "%~dp0.."

rem Override with a UNITY_EXE env var when your editor lives elsewhere.
if not defined UNITY_EXE set UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe
if not exist "%UNITY_EXE%" (
    echo Unity editor not found at %UNITY_EXE% - set the UNITY_EXE env var.
    exit /b 1
)

if not exist artifacts mkdir artifacts

set EXTRA=
if /i "%~1"=="apk" set EXTRA=-buildApk

"%UNITY_EXE%" -batchmode -quit -projectPath "%CD%" ^
    -executeMethod PuttSeed.Unity.Editor.BuildTools.BuildAndroid %EXTRA% ^
    -logFile "%CD%\artifacts\android-build.log"
set RESULT=%ERRORLEVEL%

if %RESULT% NEQ 0 (
    echo Android build FAILED ^(exit %RESULT%^). See artifacts\android-build.log
    exit /b %RESULT%
)

echo Android build succeeded. Output in artifacts\.
exit /b 0
