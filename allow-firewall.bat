@echo off
title CyhAstro - Windows Firewall Port Configurator
color 0B
echo ========================================================
echo   Configuring Windows Firewall for CyhAstro LAN Access
echo ========================================================
echo.

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Yeh script Administrator privileges maangta hai!
    echo Kripya is file par Right Click karein aur "Run as administrator" chunein.
    echo ========================================================
    pause
    exit /b 1
)

echo [1/2] Opening Port 5066 for CyhAstro Admin / Mobile App...
netsh advfirewall firewall delete rule name="CyhAstro Admin Port 5066" >nul 2>&1
netsh advfirewall firewall add rule name="CyhAstro Admin Port 5066" dir=in action=allow protocol=TCP localport=5066
if %errorLevel% equ 0 (
    echo   [OK] Port 5066 firewall me allow ho gaya!
) else (
    echo   [FAIL] Port 5066 open karne me samasya aayi.
)

echo.
echo [2/2] Opening Port 5176 for CyhAstro API...
netsh advfirewall firewall delete rule name="CyhAstro API Port 5176" >nul 2>&1
netsh advfirewall firewall add rule name="CyhAstro API Port 5176" dir=in action=allow protocol=TCP localport=5176
if %errorLevel% equ 0 (
    echo   [OK] Port 5176 firewall me allow ho gaya!
) else (
    echo   [FAIL] Port 5176 open karne me samasya aayi.
)

echo.
echo ========================================================
echo   Safalta! Mobile phone ab laptop ke server se connect ho sakta hai.
echo   Local URL: http://192.168.29.253:5066/App
echo ========================================================
pause
