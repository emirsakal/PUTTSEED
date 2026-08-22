@echo off
rem Publishes artifacts\webgl to the gh-pages branch, which GitHub Pages
rem serves at https://emirsakal.github.io/PUTTSEED/.
rem
rem   scripts\deploy-webgl.bat        stage and commit the build locally
rem   scripts\deploy-webgl.bat push   ...and push it to origin
rem
rem The demo lives on its own branch, not in main: a WebAssembly build in the
rem tree every reviewer browses is noise, and artifacts\ is gitignored exactly
rem so one never lands there by accident. gh-pages is an ORPHAN branch, with
rem no ancestor in common with main, so the demo's churn never appears in the
rem history anyone reads.
setlocal enabledelayedexpansion
cd /d "%~dp0.."

if not exist "artifacts\webgl\index.html" (
    echo No build at artifacts\webgl - run scripts\build-webgl.bat first.
    exit /b 1
)

set WORKTREE=.ghpages

rem A worktree left behind by an interrupted run would block the add.
if exist "%WORKTREE%" rmdir /s /q "%WORKTREE%"
git worktree prune

for /f "delims=" %%h in ('git rev-parse --short HEAD') do set SOURCE=%%h

git show-ref --verify --quiet refs/heads/gh-pages
if errorlevel 1 (
    echo Creating gh-pages as an orphan root...
    rem Plumbing only, so the working tree is never touched: the empty tree
    rem becomes a parentless commit and the branch points at it.
    for /f "delims=" %%t in ('git hash-object -t tree --stdin ^< nul') do set EMPTY=%%t
    for /f "delims=" %%c in ('git commit-tree !EMPTY! -m "gh-pages root"') do set ROOT=%%c
    git branch gh-pages !ROOT!
    if errorlevel 1 exit /b 1
)

git worktree add "%WORKTREE%" gh-pages
if errorlevel 1 exit /b 1

rem Clear the previous deploy first, so a file the build stopped producing
rem does not linger on the live site forever. git rm leaves .git alone.
rem
rem Both guards below are load-bearing. A pushd that fails leaves the shell
rem in the MAIN worktree, where the next two lines would delete the repo; and
rem a branch check costs nothing next to that. Destructive commands do not
rem get to assume where they are standing.
pushd "%WORKTREE%"
if errorlevel 1 (
    echo Could not enter %WORKTREE% - refusing to run a delete from elsewhere.
    exit /b 1
)
for /f "delims=" %%b in ('git rev-parse --abbrev-ref HEAD') do set BRANCH=%%b
if not "!BRANCH!"=="gh-pages" (
    echo Expected gh-pages in %WORKTREE%, found !BRANCH! - refusing to delete.
    popd
    exit /b 1
)
git rm -r --quiet --ignore-unmatch . >nul 2>&1
git clean -fdq >nul 2>&1
popd

xcopy "artifacts\webgl" "%WORKTREE%" /E /I /Y /Q >nul
if errorlevel 1 (
    echo Copy FAILED.
    exit /b 1
)

rem GitHub Pages runs Jekyll unless told otherwise, and Jekyll silently drops
rem files it does not recognise. The demo is not a blog.
type nul > "%WORKTREE%\.nojekyll"

pushd "%WORKTREE%"
if errorlevel 1 (
    echo Could not enter %WORKTREE% - refusing to commit from elsewhere.
    exit /b 1
)
git add -A
git diff --cached --quiet
if not errorlevel 1 (
    echo Nothing changed since the last deploy.
    popd
    goto :cleanup
)

git commit -q -m "deploy: WebGL demo built from %SOURCE%"
if errorlevel 1 (
    echo Commit FAILED.
    popd
    exit /b 1
)
echo Committed the demo to gh-pages ^(source %SOURCE%^).

if /i "%~1"=="push" (
    git push -u origin gh-pages
    if errorlevel 1 (
        echo Push FAILED.
        popd
        exit /b 1
    )
    echo Pushed. Enable Pages once at Settings - Pages - Branch: gh-pages / root.
) else (
    echo Not pushed. Re-run as: scripts\deploy-webgl.bat push
)
popd

:cleanup
rem The worktree is scaffolding, not a place to work: leaving it around means
rem the next run finds a dirty one.
git worktree remove --force "%WORKTREE%" >nul 2>&1
git worktree prune
exit /b 0
