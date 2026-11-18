@echo off
chcp 65001 >nul
title MilkyVerse - Chạy Hệ Thống & Selenium TestNG Automation

echo ==============================================================================
echo       MILKYVERSE - KHỞI ĐỘNG HỆ THỐNG VÀ TEST TỰ ĐỘNG (SELENIUM + TESTNG)
echo ==============================================================================
echo.
echo [BƯỚC 1/3] Khởi động Backend API (.NET Core 8)...
start "1. MilkyVerse Backend API (:5000)" cmd /k "cd /d %~dp0backend\MilkyVerse.Api && dotnet run"

echo Đang chờ Backend khởi tạo dữ liệu trong 4 giây...
timeout /t 4 /nobreak >nul

echo.
echo [BƯỚC 2/3] Khởi động Frontend Web (Next.js 16)...
start "2. MilkyVerse Frontend Web (:3000)" cmd /k "cd /d %~dp0frontend && npm run dev"

echo Đang chờ Frontend khởi động và biên dịch trong 8 giây...
timeout /t 8 /nobreak >nul

echo.
echo [BƯỚC 3/3] Khởi chạy Automation Test: Selenium + TestNG (Mở Chrome thật)...
start "3. MilkyVerse Selenium TestNG Runner" cmd /k "cd /d %~dp0automation-tests && run-selenium-tests.bat"

echo.
echo ==============================================================================
echo   ĐÃ MỞ ĐẦY ĐỦ CẢ 3 CỬA SỔ THEO ĐÚNG THỨ TỰ TRÊN MÀN HÌNH BẠN!
echo ==============================================================================
pause
