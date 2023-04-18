# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

Function IsWindows()
{
	return ([Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT)
}

if (IsWindows)
{
	Write-Host Start ETL logging
	logman create trace IotTrace -o iot.etl -pf tools/CaptureLogs/iot_providers.txt
	logman start IotTrace
}

Write-Host List installed .NET SDK versions
dotnet --list-sdks

.\build.ps1 -runCleanupSamples

$gateFailed = $LASTEXITCODE

if (IsWindows)
{
	Write-Host Stop ETL logging
	logman stop IotTrace
	logman delete IotTrace
}

if ($gateFailed)
{
	Write-Error "Cleanup was not successful, exiting..."
	exit 1
}
else
{
	Write-Host "Cleanup was successful!"
	exit 0
}