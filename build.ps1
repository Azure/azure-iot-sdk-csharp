# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#

.SYNOPSIS
Microsoft Azure IoT SDK .NET build script.

.DESCRIPTION
Builds Azure IoT SDK binaries.

Parameters:
    -clean
    -nobuild
    -notests
    -e2etests
    -configuration {Debug|Release}
    -verbosity: Sets the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].

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
    [switch] $nobuild,
    [switch] $notests,
    [switch] $e2etests,
    [string] $configuration = "Debug",
    [string] $verbosity = "q",

    # Work-in-progress switches:
    [switch] $nolegacy                   #Work to port existing projects to the .NET Core SDK. Must be set for Linux builds.
)

Function BuildProject($path, $message) {

    Write-Host
    Write-Host -ForegroundColor Cyan "BUILD: --- " $message $configuration" ---"
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
    Write-Host -ForegroundColor Cyan "TEST: --- " $message $configuration" ---"
    cd (Join-Path $rootDir $path)

    & dotnet test --verbosity normal --configuration $configuration --logger "trx"

    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed."
    }
}

Function LegacyBuildProject($path, $message) {

    Write-Host
    Write-Host -ForegroundColor Cyan "MSBUILD: --- " $message $configuration" ---"
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

Function LegacyRunTests($path, $message) {

    Write-Host
    Write-Host -ForegroundColor Cyan "MSTEST: --- " $message $configuration" ---"
    
    $container = (Split-Path -leaf $path) + ".dll"

    cd (Join-Path $rootDir (Join-Path $path "bin\$configuration"))
    & mstest /TestContainer:$container

    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed."
    }
}

$rootDir = (Get-Item -Path ".\" -Verbose).FullName

$startTime = Get-Date
$buildFailed = $true

try {

    if (-not $nobuild)
    {
        BuildProject shared\Microsoft.Azure.Devices.Shared.NetStandard "Shared Assembly"
        BuildProject device\Microsoft.Azure.Devices.Client.NetStandard "IoT Hub DeviceClient SDK"
        BuildProject service\Microsoft.Azure.Devices.NetStandard "IoT Hub ServiceClient SDK"
        BuildProject security\tpm\src "SecurityProvider for TPM"
        BuildProject provisioning\transport\amqp\src "Provisioning Transport for AMQP"
        BuildProject provisioning\transport\http\src "Provisioning Transport for HTTP"
        BuildProject provisioning\transport\mqtt\src "Provisioning Transport for MQTT"
        BuildProject provisioning\device\src "Provisioning Device Client SDK"
        BuildProject provisioning\service\src "Provisioning Service Client SDK"

        if (-not $nolegacy) 
        {
            # Build legacy project formats (Pending switch to the .NET SDK tooling.)
            LegacyBuildProject shared\build "Shared Assembly"
            LegacyBuildProject device\build "IoT Hub Device Client SDK"
            LegacyBuildProject service\build "IoT Hub Service Client SDK"
            LegacyBuildProject e2e\build "E2E Tests"

            if ($configuration.ToLower() -eq "release")
            {
                # Package creation fails in debug.
                LegacyBuildProject tools\DeviceExplorer\build "IoT Hub Device Explorer"
            }
        }
    }

    # Unit Tests require InternalsVisibleTo and can only run in Debug builds.
    if ((-not $notests) -and ($configuration.ToLower() -eq "debug"))
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "Unit Test execution"
        Write-Host

        RunTests provisioning\device\tests "Provisioning Device Client Tests"
        RunTests provisioning\transport\amqp\tests "Provisioning Transport for AMQP"
        RunTests provisioning\transport\http\tests "Provisioning Transport for HTTP"
        RunTests provisioning\transport\mqtt\tests "Provisioning Transport for MQTT"
        RunTests security\tpm\tests "SecurityProvider for TPM"

        if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows))
        {
            # TODO: #307 - DotNet fails to correctly parse the UT certificate on Linux.
            RunTests provisioning\service\tests "Provisioning Service Client Tests"
        }
        
        if (-not $nolegacy)
        {
            # Build legacy project formats (Pending switch to the .NET SDK tooling.)
            LegacyRunTests device\tests\Microsoft.Azure.Devices.Client.Test "IoT Hub Device Client Unit Tests"
            LegacyRunTests service\Microsoft.Azure.Devices\test\Microsoft.Azure.Devices.Api.Test "IoT Hub Service Client Unit Tests"
        }
    }

    if ($e2etests)
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "End-to-end Test execution"
        Write-Host

        RunTests e2e\Microsoft.Azure.Devices.E2ETests.NetStandard "IoT Hub End-to-end Tests"
        
        if ($Env:DPS_IDSCOPE -ne $null)
        {
            RunTests provisioning\e2e "Provisioning End-to-end Tests"
        }

        if (-not $nolegacy)
        {
            # Build legacy project formats (Pending switch to the .NET SDK tooling.)
            LegacyRunTests e2e\Microsoft.Azure.Devices.E2ETests "IoT Hub End-to-end Tests"
        }
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

Write-Host ("Time Elapsed {0:c}" -f ($endTime - $startTime))

if ($buildFailed) {
    Write-Host -ForegroundColor Red "Build failed."
    exit 1
}
else {
    Write-Host -ForegroundColor Green "Build succeeded."
    exit 0
}
