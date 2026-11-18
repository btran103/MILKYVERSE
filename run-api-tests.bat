@echo off
title MilkyVerse - Chay Automation Test API (Newman Postman)
echo ===================================================
echo    MILKYVERSE - KIEM THU TU DONG API (POSTMAN / NEWMAN)
echo ===================================================
echo.

cd /d "%~dp0"

echo Kiem tra va chay Newman...
npx newman run "automation-tests\postman\MilkyVerse_API.postman_collection.json" -e "automation-tests\postman\MilkyVerse_Local.postman_environment.json"

pause
