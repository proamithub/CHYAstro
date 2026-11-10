@echo off
title CyhAstro REST API
color 0A
echo ========================================================
echo   Starting CyhAstro REST API ^& Swagger UI...
echo   URL: http://localhost:5176/swagger
echo ========================================================
start "" "http://localhost:5176/swagger"
dotnet run --project CyhAstro.Api --launch-profile http
pause
