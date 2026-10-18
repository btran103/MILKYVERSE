@echo off
title MilkyVerse - Khoi Dong He Thong
echo =======================================================
echo     MILKYVERSE - VU TRU TRA SUA TICH HOP AI
echo =======================================================
echo Dang mo Backend va Frontend trong 2 cua so rieng biet...

start "MilkyVerse Backend API" cmd /k "cd /d %~dp0backend\MilkyVerse.Api && dotnet run --urls http://localhost:5000"

timeout /t 3 >nul

start "MilkyVerse Frontend Next.js" cmd /k "cd /d %~dp0frontend && npm run dev"

echo.
echo He thong da khoi dong thanh cong!
echo - Frontend: http://localhost:3000
echo - Backend API: http://localhost:5000/swagger
echo.
pause
