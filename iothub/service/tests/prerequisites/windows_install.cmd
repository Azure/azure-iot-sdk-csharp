@echo off
rem Copyright (c) Microsoft. All rights reserved.
rem Licensed under the MIT license. See LICENSE file in the project root for full license information

rem Check if running as Administrator.
net session >nul 2>&1
if not %errorlevel% == 0 (
    @echo This script must be executed from an elevated prompt.
    goto :enderror
)

set WebSocketListenPort=12346

rem Adding HTTP.SYS port reservation.
netsh http show urlacl | find ":%WebSocketListenPort%/"
if %ERRORLEVEL% == 0 (
	echo WARNING: There is already an URL reservation for port %WebSocketListenPort%.
) else (
	netsh http add urlacl url=http://+:%WebSocketListenPort%/ user=Users
)
