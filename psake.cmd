@echo off
cls
powershell.exe Set-ExecutionPolicy remotesigned 
cls
powershell -NoProfile -Command "%~dp0\psake.ps1 %*"