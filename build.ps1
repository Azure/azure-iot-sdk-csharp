# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#

.SYNOPSIS
Microsoft Azure IoT SDK .NET build script.

.DESCRIPTION
Builds Azure IoT SDK binaries.

Parameters:
    -clean: Runs dotnet clean. Use `git clean -xdf` if this is not sufficient.
    -nobuild: Skips build step (use if re-running tests after a successful build).
    -nounittests: Skips Unit Tests
    -e2etests: Runs E2E tests. Requires prerequisites and environment variables.
    -stresstests: Runs Stress tests.
    -xamarintests: Runs Xamarin tests. Requires additional SDKs and prerequisite configuration.
    -configuration {Debug|Release}
    -verbosity: Sets the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].
.NOTES
Build will automatically detect if the machine is Windows vs Unix. On Windows development boxes, additional testing on .NET Framework will be performed.

.EXAMPLE 
.\build

Builds a Debug version of the SDK.
.EXAMPLE
.\build -config Release

Builds a Release version of the SDK.
.EXAMPLE
.\build -clean -e2etests -xamarintests

Builds and runs all tests (requires prerequisites).
.EXAMPLE
.\build -nobuild -nounittests -nopackage -stresstests

Builds stress tests after a successful build.
.LINK
https://github.com/azure/azure-iot-sdk-csharp

#>

Param(
    [switch] $clean,
    [switch] $nobuild,
    [switch] $nounittests,
    [switch] $nopackage,
    [switch] $e2etests,
    [switch] $stresstests,
    [switch] $xamarintests,
    [string] $configuration = "Debug",
    [string] $verbosity = "q"
)

Function BuildProject($path, $message) {

    $label = "BUILD: --- $message $configuration ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label
    cd (Join-Path $rootDir $path)

    if ($clean) {
        & dotnet clean --verbosity $verbosity --configuration $configuration
        if ($LASTEXITCODE -ne 0) {
            throw "Clean failed: $label"
        }
    }

    & dotnet build --verbosity $verbosity --configuration $configuration

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed: $label"
    }
}

Function BuildPackage($path, $message) {
    $label = "PACK: --- $message $configuration ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label
    cd (Join-Path $rootDir $path)

    $frameworkArgs = ""

    & dotnet pack --verbosity $verbosity --configuration $configuration --no-build --include-symbols --include-source --output $localPackages

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed: $label"
    }
}

Function RunTests($path, $message, $framework="netcoreapp2.0") {

    $label = "TEST: --- $message $configuration ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label
    cd (Join-Path $rootDir $path)

    & dotnet test --framework $framework --verbosity $verbosity --configuration $configuration --logger "trx"

    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed: $label"
    }
}

Function RunApp($path, $message, $framework="netcoreapp2.0") {

    $label = "RUN: --- $message $configuration ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label
    cd (Join-Path $rootDir $path)

    & dotnet run --framework $framework --verbosity $verbosity --configuration $configuration --logger "trx"

    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed: $label"
    }
}

Function IsWindowsDevelopmentBox()
{  
    return ([Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT)    
}

$rootDir = (Get-Item -Path ".\" -Verbose).FullName
$localPackages = Join-Path $rootDir "bin\pkg"
$startTime = Get-Date
$buildFailed = $true
$errorMessage = ""

try {
    if (-not $nobuild)
    {
        # SDK binaries
        BuildProject shared\src "Shared Assembly"
        BuildProject iothub\device\src "IoT Hub DeviceClient SDK"
        BuildProject iothub\service\src "IoT Hub ServiceClient SDK"      
        BuildProject security\tpm\src "SecurityProvider for TPM"
        BuildProject provisioning\device\src "Provisioning Device Client SDK"
        BuildProject provisioning\transport\amqp\src "Provisioning Transport for AMQP"
        BuildProject provisioning\transport\http\src "Provisioning Transport for HTTP"
        BuildProject provisioning\transport\mqtt\src "Provisioning Transport for MQTT"
        BuildProject provisioning\service\src "Provisioning Service Client SDK"

        # Samples
        BuildProject iothub\device\samples "IoT Hub DeviceClient Samples"
        BuildProject iothub\service\samples "IoT Hub ServiceClient Samples"
        BuildProject provisioning\device\samples "Provisioning Device Client Samples"
        BuildProject provisioning\service\samples "Provisioning Service Client Samples"
        BuildProject security\tpm\samples "SecurityProvider for TPM Samples"

        # Xamarin samples (require Android, iOS and UWP SDKs and configured iOS remote)
        if ($xamarintests)
        {
            # TODO #335 - create new Xamarin automated samples/tests
        }
    }

    # Unit Tests require InternalsVisibleTo and can only run in Debug builds.
    if ((-not $nounittests) -and ($configuration.ToLower() -eq "debug"))
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "Unit Test execution"
        Write-Host

        RunTests iothub\device\tests "IoT Hub DeviceClient Tests"
        RunTests iothub\service\tests "IoT Hub ServiceClient Tests"
        RunTests provisioning\device\tests "Provisioning Device Client Tests"
        RunTests provisioning\transport\amqp\tests "Provisioning Transport for AMQP Tests"
        RunTests provisioning\transport\http\tests "Provisioning Transport for HTTP Tests"
        RunTests provisioning\transport\mqtt\tests "Provisioning Transport for MQTT Tests"
        RunTests security\tpm\tests "SecurityProvider for TPM Tests"
        RunTests provisioning\service\tests "Provisioning Service Client Tests"
    }
  
    if ((-not $nopackage))
    {
        BuildPackage shared\src "Shared Assembly"
        BuildPackage iothub\device\src "IoT Hub DeviceClient SDK"
        BuildPackage iothub\service\src "IoT Hub ServiceClient SDK"
        BuildPackage security\tpm\src "SecurityProvider for TPM"
        BuildPackage provisioning\device\src "Provisioning Device Client SDK"
        BuildPackage provisioning\transport\amqp\src "Provisioning Transport for AMQP"
        BuildPackage provisioning\transport\http\src "Provisioning Transport for HTTP"
        BuildPackage provisioning\transport\mqtt\src "Provisioning Transport for MQTT"
        BuildPackage provisioning\service\src "Provisioning Service Client SDK"
    }

    if ($e2etests)
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "End-to-end Test execution"
        Write-Host

        # Override verbosity to display individual test execution.
        $oldVerbosity = $verbosity
        $verbosity = "normal"
        
        RunTests e2e\test "End-to-end tests (NetCoreApp)"
        if (IsWindowsDevelopmentBox)
        {
            RunTests e2e\test "End-to-end tests (NET451)" "net451"
            RunTests e2e\test "End-to-end tests (NET47)" "net47"
        }

        $verbosity = $oldVerbosity
    }

    if ($stresstests)
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "Stress Test execution"
        Write-Host

        RunApp e2e\stress\MemoryLeakTest "MemoryLeakTest test"
    }

    # Xamarin samples (require Android, iOS and UWP SDKs and configured iOS remote)
    if ($xamarintests)
    {
        # TODO #335 - create new Xamarin automated samples/tests
    }

    $buildFailed = $false
}
catch [Exception]{
    $buildFailed = $true
    $errorMessage = $Error[0]
}
finally {
    cd $rootDir
    $endTime = Get-Date
}

Write-Host
Write-Host

Write-Host ("Time Elapsed {0:c}" -f ($endTime - $startTime))

if ($buildFailed) {
    Write-Host -ForegroundColor Red "Build failed ($errorMessage)"
    exit 1
}
else {
    Write-Host -ForegroundColor Green "Build succeeded."
    exit 0
}
