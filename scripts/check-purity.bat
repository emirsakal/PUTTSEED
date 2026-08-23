@echo off
rem Purity check: core/src must contain no float/double/System.Random/DateTime/UnityEngine.
rem Enforces CLAUDE.md hard rules 1 and 2 mechanically. Run from anywhere.
rem
rem The rule lives in tools\check-purity.py and NOT here. It used to be written
rem out twice -- once in this file, once in .github\workflows\ci.yml, which
rem called itself a mirror. Both were raw greps, so both matched the word
rem "float" inside a comment EXPLAINING that floats are banned, and CI stayed
rem red for days over a core that was clean. One rule, one implementation.
setlocal
cd /d "%~dp0.."

python tools\check-purity.py
exit /b %ERRORLEVEL%
