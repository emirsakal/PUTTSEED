@echo off
rem Batch-mode RELEASE .aab build, signed via keystore.properties (repo root,
rem gitignored) when present. Output: artifacts\PuttSeed-release.aab.
setlocal
cd /d "%~dp0.."

set UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe
if not exist "%UNITY_EXE%" (
    echo Unity editor not found at %UNITY_EXE%
    exit /b 1
)

if not exist artifacts mkdir artifacts

"%UNITY_EXE%" -batchmode -quit -projectPath "%CD%" ^
    -executeMethod PuttSeed.Unity.Editor.BuildTools.BuildAndroidRelease ^
    -logFile "%CD%\artifacts\android-release.log"
set RESULT=%ERRORLEVEL%

if %RESULT% NEQ 0 (
    echo Release build FAILED ^(exit %RESULT%^). See artifacts\android-release.log
    exit /b %RESULT%
)

echo Release build succeeded: artifacts\PuttSeed-release.aab
exit /b 0
