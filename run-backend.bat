@echo off
title MilkyVerse - Backend API
echo [MILKYVERSE] Dang khoi dong Backend ASP.NET Core Web API...
cd /d "%~dp0backend\MilkyVerse.Api"
dotnet run --urls "http://localhost:5000"
pause
