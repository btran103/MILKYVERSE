@echo off
title MilkyVerse - Chay Automation Test Playwright
echo ===================================================
echo    MILKYVERSE - HE THONG KIEM THU TU DONG (PLAYWRIGHT)
echo ===================================================
echo.

cd /d "%~dp0automation-tests\playwright"

echo [1/3] Kiem tra va cai dat thu vien Playwright...
call npm install
echo.

echo [2/3] Kiem tra trinh duyet Playwright...
call npx playwright install chromium
echo.

echo [3/3] Bat dau chay kiem thu tu dong (Giao dien UI Mode)...
echo Ban co the xem trinh duyet tu dong click kiem thu truc quan!
echo.
call npx playwright test --ui

pause
