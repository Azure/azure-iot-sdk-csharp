@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@echo off

if "%1" EQU "-?" goto get-help
if "%1" EQU "-h" goto get-help
if "%1" EQU "-help" goto get-help
if "%1" EQU "--help" goto get-help

powershell -command ".\build.ps1 %1 %2 %3 %4 %5; exit $LASTEXITCODE"
goto :eof

:get-help
powershell -command "Get-Help .\build.ps1"
