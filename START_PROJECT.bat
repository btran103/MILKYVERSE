@echo off
chcp 65001 >nul
title MilkyVerse - Khởi Động Dự Án (Backend + Frontend + Trình Duyệt)

echo ==============================================================================
echo            MILKYVERSE - KHỞI CHẠY TOÀN BỘ DỰ ÁN (BACKEND & FRONTEND)
echo ==============================================================================
echo.

:: Dừng tiến trình Backend cũ (nếu có) để nạp code mới nhất
taskkill /F /IM MilkyVerse.Api.exe >nul 2>nul

:: Tự động nạp API Key & SMTP Password từ file .env.local (file này đã được ẩn trong .gitignore, không bị lộ khi push Git)
if exist "%~dp0.env.local" (
    for /f "usebackq tokens=1,* delims==" %%A in ("%~dp0.env.local") do (
        if not "%%A"=="" if not "%%A:~0,1%"=="#" set "%%A=%%B"
    )
)

echo [1/3] Đang khởi động Backend API (.NET Core - http://localhost:5000)...
start "1. MilkyVerse Backend API (:5000)" cmd /k "cd /d %~dp0backend\MilkyVerse.Api && dotnet run"

echo Đang chờ Backend nạp CSDL trong 4 giây...
timeout /t 4 /nobreak >nul

:: Kiểm tra nếu Frontend (:3000) chưa chạy thì bật lên
netstat -ano | findstr ":3000" >nul
if %errorlevel% neq 0 (
    echo [2/3] Đang khởi động Frontend Web (Next.js - http://localhost:3000)...
    start "2. MilkyVerse Frontend Web (:3000)" cmd /k "cd /d %~dp0frontend && npm run dev"
    timeout /t 5 /nobreak >nul
) else (
    echo [2/3] Frontend Web đang chạy sẵn tại http://localhost:3000!
)

echo [3/3] Đang mở trình duyệt tới http://localhost:3000...
start http://localhost:3000

echo.
echo ==============================================================================
echo   DỰ ÁN ĐÃ KHỞI CHẠY THÀNH CÔNG!
echo   - Giao diện Web: http://localhost:3000
echo   - Backend API:   http://localhost:5000
echo ==============================================================================
pause
