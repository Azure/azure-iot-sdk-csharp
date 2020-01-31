@rem Copyright (c) Microsoft. All rights reserved.
@rem Licensed under the MIT license. See LICENSE file in the project root for full license information.

setlocal

set build-root=%~dp0..

@rem resolve to fully qualified path
for %%i in ("%build-root%") do set build-root=%%~fi

cd %build-root%

@rem Build Debug + run UTs and select E2E
taskkill /F /IM Simulator.exe
start /D .\vsts\TpmSimulator Simulator.exe
call build.cmd -clean -build -configuration Debug -prtests
if errorlevel 1 goto :eof

@rem If the build is Delay Signed, this requires strong name validation disabled for our key:
@rem (As Administrator:)  sn -Vr *,31bf3856ad364e35	
@rem Re-enable strong name validation for this key using:	
@rem (As Administrator:)  sn -Vx

echo.
echo C# build completed successfully
echo.
