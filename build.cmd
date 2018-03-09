@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@echo off

if "%1" EQU "-?" goto get-help
if "%1" EQU "-h" goto get-help
if "%1" EQU "-help" goto get-help
if "%1" EQU "--help" goto get-help

powershell -noprofile -command ".\build.ps1 %*; exit $LASTEXITCODE"
exit /b %ERRORLEVEL%

:get-help
powershell -noprofile -command "Get-Help .\build.ps1"
