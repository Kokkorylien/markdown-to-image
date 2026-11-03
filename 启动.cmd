@echo off
setlocal
cd /d "%~dp0"
if exist "release\MarkdownToImage-light-win32-x64\MarkdownToImage.exe" (
  start "" "release\MarkdownToImage-light-win32-x64\MarkdownToImage.exe"
  exit /b
)
where node.exe >nul 2>nul
if errorlevel 1 (
  echo Please install Node.js 22.12 or later, then run this file again.
  pause
  exit /b 1
)
if not exist "node_modules\.bin\esbuild.cmd" (
  call npm.cmd ci
  if errorlevel 1 (
    pause
    exit /b 1
  )
)
call npm.cmd start
