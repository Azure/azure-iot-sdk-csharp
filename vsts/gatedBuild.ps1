# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

Param(
    [string] $configuration = "Debug",
    [string] $framework = "*"
)

Write-Host List active docker containers
docker ps -a

#Load functions used to check what, if any, e2e tests should be run
. .\vsts\determine_tests_to_run.ps1

$runTestCmd = ".\build.ps1 -clean -build -configuration $configuration -prtests -framework $framework"
if (ShouldSkipDPSTests)
{
	Write-Host "Will skip DPS tests"
	$runTestCmd += " -skipDPSTests"
}
else
{
	Write-Host "Will run DPS tests"
}

if (ShouldSkipIotHubTests)
{
	Write-Host "Will skip Iot Hub tests"
	$runTestCmd += " -skipIoTHubTests"
}
else
{
	Write-Host "Will run Iot Hub tests"
}

Write-Host "Starting tests..."

# Run the build.ps1 script with the above parameters
Invoke-Expression $runTestCmd

if ($LASTEXITCODE -eq 0)
{
	Write-Host "Testing was successful!"
	exit 0
}
else
{
	Write-Error "Testing was not successful, exiting..."
	exit 1
}