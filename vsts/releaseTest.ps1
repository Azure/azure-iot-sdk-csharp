# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
Function IsWindows() 
{
	return ([Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT)
}

if (isWindows) 
{
	Write-Host Start ETL logging
	logman create trace IotTrace -o iot.etl -pf tools/CaptureLogs/iot_providers.txt
	logman start IotTrace
}
else
{
	Write-Host Install nuget CLI for Linux
	sudo apt install nuget
}

Write-Host List active docker containers
docker ps -a

Write-Host Add DevOps artifacts location as local NuGet source
nuget sources Add -Name "LocalPackages" -Source $env:AZURE_IOT_LOCALPACKAGES

Write-Host List all NuGet sources
nuget sources List

$runTestCmd = ".\build.ps1 -build -clean -configuration RELEASE -framework $env:FRAMEWORK -e2etests"

Invoke-Expression $runTestCmd

$gateFailed = $LASTEXITCODE

if (isWindows) 
{
	Write-Host Stop ETL logging
	logman stop IotTrace
	logman delete IotTrace
}

if ($gateFailed) 
{
	Write-Error "Testing was not successful, exiting..."
	exit 1
}
else 
{
	Write-Host "Testing was successful!"
	exit 0
}