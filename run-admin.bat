@echo off
title CyhAstro Admin Web Portal
color 0B
echo ========================================================
echo   Starting CyhAstro Admin Web Portal...
echo   URL: http://localhost:5066/Admin/Login
echo   Default Admin: admin@cyhastro.com / Admin@123
echo ========================================================
start "" "http://localhost:5066/Admin/Login"
dotnet run --project CyhAstro.Admin --launch-profile http
pause
