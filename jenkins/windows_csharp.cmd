@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

setlocal

rem // default build options
set skip-ut=0

if "%1" equ "--skip-ut" (
    set skip-ut=1
)

set build-root=%~dp0..
rem // resolve to fully qualified path
for %%i in ("%build-root%") do set build-root=%%~fi

REM -- C# Shared Assembly --
cd %build-root%\shared\build
call build.cmd
if errorlevel 1 goto :eof
cd %build-root%

REM -- C# Device SDK --
cd %build-root%\device\build
call build.cmd
if errorlevel 1 goto :eof
cd %build-root%

REM -- C# Service SDK --
cd %build-root%\service\build
call build.cmd
if errorlevel 1 goto :eof
cd %build-root%

REM -- C# E2E Tests  --
cd %build-root%\e2e\build
call build.cmd
if errorlevel 1 goto :eof
cd %build-root%

REM -- Device Explorer --
cd %build-root%\tools\DeviceExplorer\build
call build.cmd
if errorlevel 1 goto :eof
cd %build-root%

if %skip-ut%==0 (
    REM -- Run C# device SDK unit Tests  --
    cd %build-root%\device\tests\Microsoft.Azure.Devices.Client.Test\bin\Release
    mstest /TestContainer:Microsoft.Azure.Devices.Client.Test.dll
    if errorlevel 1 goto :eof
    cd %build-root%
)

REM -- Run C# E2E Tests  --
cd %build-root%\e2e\Microsoft.Azure.Devices.E2ETests\bin\Release
mstest /TestContainer:Microsoft.Azure.Devices.E2ETests.dll /resultsfile:testResults.trx
if errorlevel 1 goto :eof
cd %build-root%

echo.
echo C# build completed successfully
echo.

