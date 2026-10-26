@echo off
title AI & DS Department // 3D AR Timetable Demo Launcher
color 0B
cls
echo ======================================================================
echo    DEPARTMENT OF ARTIFICIAL INTELLIGENCE & DATA SCIENCE
echo    3D AUGMENTED REALITY TIMETABLE // PROJECT DEMO LAUNCHER
echo ======================================================================
echo.
echo [1/2] Opening Interactive 3D Timetable Simulation in Browser...
start "" "%~dp0WebAR_Live_Demo\preview.html"
echo [OK] Interactive 3D Board loaded.
echo.
echo [2/2] Starting Local Dual HTTP/HTTPS Mobile AR Server...
cd /d "%~dp0WebAR_Live_Demo"
python serve_webar.py
pause
