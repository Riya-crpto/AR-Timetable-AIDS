@echo off
title Push AI & DS AR Timetable to GitHub
color 0A
cls
echo ======================================================================
echo    PUSH TO GITHUB // DEPARTMENT OF AI & DATA SCIENCE
echo ======================================================================
echo.
echo Step 1: Please enter your GitHub Repository URL.
echo Example: https://github.com/your-username/AR-Timetable-AIDS.git
echo.
set /p REPO_URL="Enter Git Remote URL: "

if "%REPO_URL%"=="" (
    echo [ERROR] No URL entered. Aborting.
    pause
    exit /b
)

echo.
echo Configuring remote origin...
git remote remove origin 2>nul
git remote add origin %REPO_URL%
git branch -M main

echo.
echo Pushing code to GitHub...
git push -u origin main

if %errorlevel% equ 0 (
    echo.
    echo ======================================================================
    echo    SUCCESS! Project successfully pushed to your GitHub repository!
    echo ======================================================================
    echo.
    echo Next step to get your LIVE WEB LINK:
    echo 1. Go to your repo on github.com
    echo 2. Click Settings -> Pages (left sidebar)
    echo 3. Under Branch, select 'main' and '/ (root)', then click Save!
    echo.
) else (
    echo.
    echo [ERROR] Push failed. Check your GitHub credentials / URL.
)
pause
