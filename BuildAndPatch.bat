@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"
title Il2CppInterop x86 XREF Patcher - Universal

 echo ================================================
 echo Il2CppInterop x86 XREF Patcher - Universal
 echo MelonLoader / Il2CppInterop
 echo ================================================
 echo.

set "DOTNET=C:\Program Files\dotnet\dotnet.exe"
if not exist "%DOTNET%" set "DOTNET=C:\Program Files (x86)\dotnet\dotnet.exe"
if not exist "%DOTNET%" set "DOTNET=dotnet"

"%DOTNET%" --list-sdks >nul 2>&1
if errorlevel 1 (
  echo ERROR: No .NET SDK was found.
  echo Install a .NET SDK 8.x or newer, then run this again.
  pause
  exit /b 1
)

echo Enter the full path to Il2CppInterop.Generator.dll
set /p "TARGET=Target DLL: "
set "TARGET=%TARGET:"=%"
if not exist "%TARGET%" (
  echo ERROR: Target DLL not found:
  echo %TARGET%
  pause
  exit /b 1
)

for %%F in ("%TARGET%") do set "TARGET_DIR=%%~dpF"
set "DEFAULT_CECIL=%TARGET_DIR%Mono.Cecil.dll"

echo.
echo Enter the full path to Mono.Cecil.dll
if exist "%DEFAULT_CECIL%" (
  echo Press Enter to use the copy next to the target DLL:
  echo %DEFAULT_CECIL%
)
set /p "CECIL=Mono.Cecil.dll: "
set "CECIL=%CECIL:"=%"
if not defined CECIL set "CECIL=%DEFAULT_CECIL%"
if not exist "%CECIL%" (
  echo ERROR: Mono.Cecil.dll not found:
  echo %CECIL%
  pause
  exit /b 1
)

if not exist "build\Patcher.cs" copy /Y "%~dp0Patcher.cs" "build\Patcher.cs" >nul

 echo.
 echo Building patcher...
"%DOTNET%" build "build\Patcher.csproj" -c Release --nologo /p:CecilPath="%CECIL%"
if errorlevel 1 (
  echo.
  echo BUILD FAILED.
  pause
  exit /b 1
)

echo.
echo Patching target DLL...
"%DOTNET%" "build\bin\Release\net8.0\Patcher.dll" "%TARGET%"
if errorlevel 1 (
  echo.
  echo PATCH FAILED.
  echo Your original DLL should remain recoverable if a .backup exists.
  pause
  exit /b 1
)

echo.
echo ================================================
echo PATCH COMPLETE
echo ================================================
echo.
echo x86: Pass16 XREF scanning is skipped.
echo x64: Pass16 XREF scanning remains enabled.
echo.
pause
