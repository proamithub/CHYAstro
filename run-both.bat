@echo off
title CyhAstro Full Stack Launcher
color 0E
echo ========================================================
echo   Launching CyhAstro API + Admin Portal...
echo ========================================================
start "CyhAstro API (Port 5176)" cmd /k "title CyhAstro API && cd /d "%~dp0" && dotnet run --project CyhAstro.Api --launch-profile http"
timeout /t 4 /nobreak >nul
start "CyhAstro Admin (Port 5066)" cmd /k "title CyhAstro Admin && cd /d "%~dp0" && dotnet run --project CyhAstro.Admin --launch-profile http"
timeout /t 3 /nobreak >nul
start "" "http://localhost:5066/Admin/Login"
start "" "http://localhost:5176/swagger"
echo ========================================================
echo   Both services started in separate console windows!
echo   - Admin Portal: http://localhost:5066/Admin/Login
echo   - REST API:     http://localhost:5176/swagger
echo ========================================================
timeout /t 5
