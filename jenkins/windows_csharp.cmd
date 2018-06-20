@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

setlocal

set build-root=%~dp0..
rem // resolve to fully qualified path
for %%i in ("%build-root%") do set build-root=%%~fi

cd %build-root%

rem Build Debug + run UTs
call build.cmd -clean -configuration Debug
if errorlevel 1 goto :eof

rem Run the TPM Simulator
taskkill /F /IM Simulator.exe
start /D %tpm_simulator_path% simulator.exe

rem Build Release + run E2E Tests
rem If the build is Delay Signed, this requires strong name validation disabled for our key:
rem (As Administrator:)  sn -Vr *,31bf3856ad364e35
rem Re-enable strong name validation for this key using:
rem (As Administrator:)  sn -Vx
call build.cmd -clean -configuration Release -e2etests
if errorlevel 1 goto :eof

echo.
echo C# build completed successfully
echo.
