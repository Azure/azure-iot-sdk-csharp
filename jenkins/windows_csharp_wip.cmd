@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

setlocal

set build-root=%~dp0..
rem // resolve to fully qualified path
for %%i in ("%build-root%") do set build-root=%%~fi

cd %build-root%
call build.cmd -clean -configuration Release -wip_provisioning 

rem TODO: Disabling E2E .Net Standard tests in Jenkins.
rem -e2etests

if errorlevel 1 goto :eof

echo.
echo C# build completed successfully
echo.
