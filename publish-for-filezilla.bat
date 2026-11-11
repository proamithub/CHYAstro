@echo off
title CyhAstro - Publish for FileZilla (FTP / Hosting)
color 0A
echo ========================================================
echo   CyhAstro Production Publisher (For FileZilla / FTP)
echo ========================================================
echo.
echo [1/4] Cleaning old publish folders...
if exist ".\publish" rmdir /s /q ".\publish"
mkdir ".\publish\Admin"
mkdir ".\publish\Api"

echo.
echo [2/4] Publishing CyhAstro.Admin (Admin Portal + Mobile /App)...
dotnet publish CyhAstro.Admin\CyhAstro.Admin.csproj -c Release -o ".\publish\Admin"
if %errorLevel% neq 0 (
    echo [ERROR] Admin build fail ho gaya!
    pause
    exit /b 1
)

echo.
echo [3/4] Publishing CyhAstro.Api (REST API Service)...
dotnet publish CyhAstro.Api\CyhAstro.Api.csproj -c Release -o ".\publish\Api"
if %errorLevel% neq 0 (
    echo [ERROR] API build fail ho gaya!
    pause
    exit /b 1
)

echo.
echo [4/4] Verifying web.config for IIS Hosting...
if not exist ".\publish\Admin\web.config" (
    echo Creating web.config for Admin...
    (
        echo ^<?xml version="1.0" encoding="utf-8"?^>
        echo ^<configuration^>
        echo   ^<location path="." inheritInChildApplications="false"^>
        echo     ^<system.webServer^>
        echo       ^<handlers^>
        echo         ^<add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" /^>
        echo       ^<handlers^>
        echo       ^<aspNetCore processPath="dotnet" arguments=".\CyhAstro.Admin.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess"^>
        echo         ^<environmentVariables^>
        echo           ^<environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" /^>
        echo         ^</environmentVariables^>
        echo       ^</aspNetCore^>
        echo     ^</system.webServer^>
        echo   ^</location^>
        echo ^</configuration^>
    ) > ".\publish\Admin\web.config"
)

echo.
echo ========================================================
echo   Safalta! Production Build Ready ho gaya hai!
echo ========================================================
echo   Publish Folder Path:
echo   - Admin Portal: %~dp0publish\Admin
echo   - REST API:     %~dp0publish\Api
echo.
echo   FileZilla me kaise upload karein:
echo   1. FileZilla kholein aur FTP Host, Username, Password dalein.
echo   2. Server par apne domain ke folder (e.g. httpdocs ya public_html) me jayein.
echo   3. 'publish\Admin' ke andar ke sabhi files ko wahan drag-drop kar dein!
echo ========================================================
echo Opening publish folder in Windows Explorer...
explorer ".\publish"
pause
