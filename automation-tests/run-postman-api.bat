@echo off
chcp 65001 >nul
echo ========================================================
echo   [MilkyVerse] Chạy Automation Test API (Postman Collection)
echo ========================================================
cd /d "%~dp0"
call npx -y newman run postman/MilkyVerse_API.postman_collection.json -e postman/MilkyVerse_Local.postman_environment.json
pause
