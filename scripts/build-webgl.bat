@echo off
rem Batch-mode WebGL build for the playable demo. Output: artifacts\webgl\.
rem
rem This one is SLOW - IL2CPP has to compile the whole game to WebAssembly,
rem which takes tens of minutes on a cold Library. Nothing is wrong if it
rem sits quiet; watch artifacts\webgl-build.log.
rem
rem The Unity editor must be CLOSED: batch mode cannot take the project lock.
setlocal
cd /d "%~dp0.."

rem Override with a UNITY_EXE env var when your editor lives elsewhere.
if not defined UNITY_EXE set UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe
if not exist "%UNITY_EXE%" (
    echo Unity editor not found at %UNITY_EXE% - set the UNITY_EXE env var.
    exit /b 1
)

if not exist artifacts mkdir artifacts

"%UNITY_EXE%" -batchmode -quit -projectPath "%CD%" ^
    -buildTarget WebGL ^
    -executeMethod PuttSeed.Unity.Editor.BuildTools.BuildWebGL ^
    -logFile "%CD%\artifacts\webgl-build.log"
set RESULT=%ERRORLEVEL%

if %RESULT% NEQ 0 (
    echo WebGL build FAILED ^(exit %RESULT%^). See artifacts\webgl-build.log
    exit /b %RESULT%
)

echo WebGL build succeeded. Output in artifacts\webgl\.
echo Serve it locally with:  npx --yes serve artifacts\webgl
exit /b 0
