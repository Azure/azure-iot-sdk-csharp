@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

setlocal

set build-root=%~dp0..
rem // resolve to fully qualified path
for %%i in ("%build-root%") do set build-root=%%~fi

cd %build-root%

rem Handle Windows Desktop vs Windows IoT Core (distinguished by HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\EditionID)
for /f "tokens=3" %%a in ('reg query "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion"  /V EditionID  ^|findstr /ri "REG_SZ"') do set EditionID=%%a

if "%EditionID%"=="IoTUAP" (
    rem Windows IoT Core: run UTs
    rem     parameter -nobuild won't force a re-build if a successful build already exists (it will still do a build if no successful one exists already)
    rem     parameter -nopackage avoids building NuGet package - Windows IoT Core is not a dev environment that can do that
    call build.cmd -clean -configuration Debug -nobuild -nopackage
    if errorlevel 1 goto :eof
    
    taskkill /F /IM Simulator.exe
    start /D C:\Data\WolfRelease C:\Data\WolfRelease\Simulator.exe
    
    rem Windows IoT Core: run E2E Tests
    call build.cmd -clean -configuration Release -e2etests -nobuild -nopackage
    if errorlevel 1 goto :eof
) else (
    rem Build Debug + run UTs
    call build.cmd -clean -configuration Debug
    if errorlevel 1 goto :eof
    
    taskkill /F /IM Simulator.exe
    start /D .\vsts\TpmSimulator Simulator.exe
    
    rem Build Release + run E2E Tests
    rem If the build is Delay Signed, this requires strong name validation disabled for our key:
    rem (As Administrator:)  sn -Vr *,31bf3856ad364e35
    rem Re-enable strong name validation for this key using:
    rem (As Administrator:)  sn -Vx
    call build.cmd -clean -configuration Release -e2etests
    if errorlevel 1 goto :eof
)

echo.
echo C# build completed successfully
echo.
