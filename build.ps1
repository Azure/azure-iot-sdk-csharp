# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#

.SYNOPSIS
Microsoft Azure IoT SDK .NET build script.

.DESCRIPTION
Builds Azure IoT SDK binaries.

Parameters:
    -clean
    -notests
    -e2etests
    -configuration {Debug|Release}
    -verbosity: Sets the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].

    -wip_provisioning: Builds the Device Provisioning Service SDK (work-in-progress)
    -nolegacy: Skips .Net Framework based builds. This is used to build on non-Windows OSs.

.EXAMPLE
.\build.ps1

.NOTES
This is work-in-progress. We are currently migrating to the new .NET Core SDK toolset for builds.

.LINK
https://github.com/azure/azure-iot-sdk-csharp

#>

Param(
    [switch] $clean,
    [switch] $notests,
    [switch] $e2etests,
    [string] $configuration = "Debug",
    [string] $verbosity = "q",

    # Work-in-progress switches:
    [switch] $wip_provisioning = $false, #Work for the Device Provisioning Service.
    [switch] $nolegacy                   #Work to port existing projects to the .NET Core SDK. Must be set for Linux builds.
)

Function BuildProject($path, $message) {

    Write-Host
    Write-Host -ForegroundColor Cyan "BUILD: --- " $message " ---"
    cd (Join-Path $rootDir $path)

    if ($clean) {
        & dotnet clean --verbosity $verbosity --configuration $configuration
        if ($LASTEXITCODE -ne 0) {
            throw "Clean failed."
        }
    }

    & dotnet build --verbosity $verbosity --configuration $configuration

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed."
    }
}

Function RunTests($path, $message) {

    Write-Host
    Write-Host -ForegroundColor Cyan "TEST: --- " $message " ---"
    cd (Join-Path $rootDir $path)

    & dotnet test --verbosity normal --configuration $configuration --logger "trx"

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed."
    }
}

Function LegacyBuildProject($path, $message) {

    Write-Host
    Write-Host -ForegroundColor Cyan "MSBUILD: --- " $message " ---"
    cd (Join-Path $rootDir $path)

    $commandLine = ".\build.cmd --config $configuration"
    
    if ($clean) {
        $commandLine += " -c"
    }

    cmd /c "$commandLine && exit /b !ERRORLEVEL!"

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed."
    }
}

# Set WIP environment variables.
if ($wip_provisioning -eq $true) {
    Write-Host -ForegroundColor Magenta "!!! Building work in progress: Provisioning !!!"
    $env:WIP_PROVISIONING = "true"
}

$rootDir = (Get-Item -Path ".\" -Verbose).FullName

$startTime = Get-Date
$buildFailed = $true

try {

    BuildProject shared\Microsoft.Azure.Devices.Shared.NetStandard "Shared Assembly"
    BuildProject device\Microsoft.Azure.Devices.Client.NetStandard "IoT Hub Device SDK"
    BuildProject service\Microsoft.Azure.Devices.NetStandard "IoT Hub Service SDK"

    if (-not $nolegacy) {
        # Build legacy project formats (Pending switch to the .NET SDK tooling.)
        LegacyBuildProject shared\build "Shared Assembly"
        LegacyBuildProject device\build "Iot Hub Device SDK"
        LegacyBuildProject service\build "Iot Hub Service SDK"
        LegacyBuildProject e2e\build "E2E Tests"
        LegacyBuildProject tools\DeviceExplorer\build "DeviceExplorer"
    }

    # Work-in-progress build:

    if ($wip_provisioning) {
        BuildProject provisioning\device "Provisioning Device SDK"
        BuildProject provisioning\service "Provisioning Service SDK"
        BuildProject provisioning\transport\amqp "Provisioning Transport for AMQP"
        BuildProject provisioning\transport\http "Provisioning Transport for HTTP"
        BuildProject provisioning\transport\mqtt "Provisioning Transport for MQTT"

        BuildProject security\tpm "SecurityClient for TPM"
    }

    if (-not $notests)
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "Unit Test execution"
        Write-Host

        if ($wip_provisioning)
        {
            RunTests provisioning\device\tests "Provisioning Device Tests"
            RunTests provisioning\service\tests "Provisioning Device Tests"
            RunTests provisioning\transport\amqp\tests "Provisioning Transport for AMQP"
            RunTests provisioning\transport\http\tests "Provisioning Transport for HTTP"
            RunTests provisioning\transport\mqtt\tests "Provisioning Transport for MQTT"
            
            RunTests security\tpm\tests "SecurityClient for TPM"
        }
    }

    if ($e2etests)
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "End-to-end Test execution"
        Write-Host

        RunTests e2e\Microsoft.Azure.Devices.E2ETests.NetStandard "End-to-end Tests"
    }

    $buildFailed = $false
}
catch [Exception]{
    $buildFailed = $true
    if ($verbosity -ne "q") {
        Write-Error $Error[0]
    }
}
finally {
    cd $rootDir
    $endTime = Get-Date
}

Write-Host
Write-Host

if ($buildFailed) {
    Write-Host -ForegroundColor Red "Build failed."
}
else {
    Write-Host -ForegroundColor Green "Build succeeded."
}

Write-Host ("Time Elapsed {0:c}" -f ($endTime - $startTime))
