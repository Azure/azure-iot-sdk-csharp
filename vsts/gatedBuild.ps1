# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

Function IsWindows()
{
	return ([Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT)
}

if ($env:SHOULD_RUN -eq "False")
{
	Write-Host "Instructed not to run '$($env:FRAMEWORK)' due to SHOULD_RUN being '$($env:SHOULD_RUN)'. Quitting."
	exit 0
}

if (IsWindows)
{
	Write-Host Start ETL logging
	logman create trace IotTrace -o iot.etl -pf tools/CaptureLogs/iot_providers.txt
	logman start IotTrace
}

Write-Host List active docker containers
docker ps -a

Write-Host List installed .NET SDK versions
dotnet --list-sdks

#Load functions used to check what, if any, e2e tests should be run
. .\vsts\determine_tests_to_run.ps1

$runTestCmd = ".\build.ps1 -clean -build -configuration DEBUG -framework $($env:FRAMEWORK) -noBuildBeforeTesting -runSamples"
if (IsPullRequestBuild)
{
	Write-Host "Pull request build detected, will run pr tests"
	$runTestCmd += " -prtests"

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
}
else
{
	#Likely a nightly or CI build
	Write-Host "Not a pull request build, will run all tests"
	$runTestCmd += " -unittests -e2etests -runSamples"
}

Write-Host "Starting tests... with '$runTestCmd'"

# Run the build.ps1 script with the above parameters
Invoke-Expression $runTestCmd

$gateFailed = $LASTEXITCODE

if (IsWindows)
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