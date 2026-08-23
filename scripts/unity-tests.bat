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

rem A green test run is not a green build. The test assembly does not
rem reference the Editor one, so a broken editor script compiles to nothing,
rem runs no tests, and reports success -- which is exactly what happened to a
rem ScreenshotTool edit that did not compile while this said 178/178 passed.
findstr /c:"error CS" "%CD%\artifacts\unity-tests.log" >nul 2>&1
if not errorlevel 1 (
    echo.
    echo COMPILE ERRORS - the tests passed but something did not build:
    findstr /c:"error CS" "%CD%\artifacts\unity-tests.log"
    exit /b 1
)

echo EditMode tests passed, everything compiled.
exit /b 0
