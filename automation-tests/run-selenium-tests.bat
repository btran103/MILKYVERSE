@echo off
chcp 65001 >nul
title MilkyVerse - Selenium + TestNG Automation Tests
echo ======================================================================
echo   [MilkyVerse] KHỞI CHẠY AUTOMATION TEST: SELENIUM + TESTNG
echo   (Trình duyệt Chrome sẽ tự động bật lên và kiểm thử trước mắt bạn)
echo ======================================================================

set "PATH=C:\tools\apache-maven-3.9.9\bin;%PATH%"

:: Kiểm tra nếu Backend (:5000) chưa chạy thì tự bật
netstat -ano | findstr ":5000" >nul
if %errorlevel% neq 0 (
    echo [THÔNG BÁO] Chưa thấy Backend chạy (:5000). Đang tự động bật Backend...
    start "MilkyVerse Backend API (:5000)" cmd /k "cd /d %~dp0..\backend\MilkyVerse.Api && dotnet run"
    timeout /t 3 /nobreak >nul
)

:: Kiểm tra nếu Frontend (:3000) chưa chạy thì tự bật
netstat -ano | findstr ":3000" >nul
if %errorlevel% neq 0 (
    echo [THÔNG BÁO] Chưa thấy Frontend chạy (:3000). Đang tự động bật Frontend...
    start "MilkyVerse Frontend Web (:3000)" cmd /k "cd /d %~dp0..\frontend && npm run dev"
    echo Đang đợi Frontend sẵn sàng trong 8 giây...
    timeout /t 8 /nobreak >nul
)

cd /d "%~dp0selenium-testng"
echo.
echo [BẮT ĐẦU TEST] Đang biên dịch mã nguồn và mở Chrome...
call mvn clean test
pause
