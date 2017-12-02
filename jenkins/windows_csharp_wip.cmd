@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

setlocal

set build-root=%~dp0..
rem // resolve to fully qualified path
for %%i in ("%build-root%") do set build-root=%%~fi

cd %build-root%
call build.cmd -clean -configuration Release -wip_provisioning -e2etests
if errorlevel 1 goto :eof

REM -- Run C# device SDK unit Tests  --
REM cd %build-root%\device\tests\Microsoft.Azure.Devices.Client.Test\bin\Release
REM mstest /TestContainer:Microsoft.Azure.Devices.Client.Test.dll
REM if errorlevel 1 goto :eof
REM cd %build-root%

REM -- Run C# E2E Tests  --
cd %build-root%\e2e\Microsoft.Azure.Devices.E2ETests\bin\Release
mstest /TestContainer:Microsoft.Azure.Devices.E2ETests.dll /resultsfile:testResults.trx
if errorlevel 1 goto :eof
cd %build-root%

echo.
echo C# build completed successfully
echo.
