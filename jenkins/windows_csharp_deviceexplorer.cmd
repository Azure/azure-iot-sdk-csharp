@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

setlocal

set build-root=%~dp0..
rem // resolve to fully qualified path
for %%i in ("%build-root%") do set build-root=%%~fi

cd %build-root%

pushd tools\DeviceExplorer
call build.cmd -clean -configuration Release
if errorlevel 1 goto :err

echo.
echo C# build completed successfully
echo.

:err
popd
