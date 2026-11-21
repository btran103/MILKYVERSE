@echo off
chcp 65001 >nul
title MilkyVerse - Ngrok Payment Webhook Tunnel (Port 5000)
echo ============================================================================
echo   [MILKYVERSE] KHỞI CHẠY NGROK WEBHOOK XÁC THỰC THANH TOÁN ONLINE (:5000)
echo ============================================================================
echo.
where ngrok >nul 2>nul
if %errorlevel% neq 0 (
    echo [HƯỚNG DẪN CÀI ĐẶT NHANH NGROK]
    echo Máy của bạn chưa cài lệnh 'ngrok'. Bạn có thể cài đặt trong 10 giây bằng lệnh:
    echo    winget install Ngrok.Ngrok
    echo Hoặc tải tại: https://ngrok.com/download
    echo.
    echo Sau khi cài xong, đăng nhập token miễn phí:
    echo    ngrok config add-authtoken ^<TOKEN_CUA_BAN^>
    echo.
    pause
    exit /b 1
)

echo Đang mở đường hầm HTTPS Public tới Backend MilkyVerse (http://localhost:5000)...
echo Giao diện quản lý Webhook Ngrok: http://127.0.0.1:4040
echo.
ngrok http 5000
pause
