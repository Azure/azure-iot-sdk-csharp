# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#

.SYNOPSIS
Microsoft Azure IoT SDK .NET Samples build script.

.DESCRIPTION
Builds Azure IoT SDK samples.

Parameters:
    -clean: Runs dotnet clean. Use `git clean -xdf` if this is not sufficient.
    -build: Builds the project.
    -run: Runs the sample. The required environmental variables need to be set beforehand.
    -configuration {Debug|Release}
    -verbosity: Sets the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].

.EXAMPLE
.\build

Builds a Debug version of the SDK.
.EXAMPLE
.\build -config Release

Builds a Release version of the SDK.
.EXAMPLE
.\build -clean

.LINK
https://github.com/Azure-Samples/azure-iot-samples-csharp
https://github.com/azure/azure-iot-sdk-csharp

#>

Param(
    [switch] $clean,
    [switch] $build,
    [switch] $run,
    [string] $configuration = "Debug",
    [string] $verbosity = "q"
)

Function IsWindowsDevelopmentBox()
{
    return ([Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT)
}

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

Function RunApp($path, $message, $params) {

    $label = "RUN: --- $message $configuration ($params)---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label
    cd (Join-Path $rootDir $path)

    $runCommand = "dotnet run -- $params"
    Invoke-Expression $runCommand

    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed: $label"
    }
}

$rootDir = (Get-Item -Path ".\" -Verbose).FullName
$startTime = Get-Date
$buildFailed = $true
$errorMessage = ""

try {
    if ($build)
    {
        BuildProject iot-hub\Samples\device "IoTHub Device Samples"
        BuildProject iot-hub\Samples\module "IoTHub Module Samples"
        BuildProject iot-hub\Samples\service "IoTHub Service Samples"
        BuildProject iot-hub\Quickstarts "IoTHub Device Quickstarts"
        BuildProject iot-hub\Tutorials\Routing "IoTHub Tutorials - Routing"
        BuildProject provisioning\Samples\device "Provisioning Device Samples"
        BuildProject provisioning\Samples\service "Provisioning Service Samples"
        BuildProject security\Samples "Security Samples"
    }

    if ($run)
    {
        $sampleRunningTime = 60

        # Run cleanup first so the samples don't get overloaded with old devices
        RunApp provisioning\Samples\service\CleanupEnrollmentsSample "Provisioning\Service\CleanupEnrollmentsSample"
        RunApp iot-hub\Samples\service\CleanUpDevicesSample "IoTHub\Service\CleanUpDevicesSample"

        RunApp iot-hub\Samples\device\DeviceReconnectionSample "IoTHub\Device\DeviceReconnectionSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"" -r $sampleRunningTime"
        RunApp iot-hub\Samples\device\FileUploadSample "IoTHub\Device\FileUploadSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"""
        RunApp iot-hub\Samples\device\MessageReceiveSample "IoTHub\Device\MessageReceiveSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"""
        RunApp iot-hub\Samples\device\MethodSample "IoTHub\Device\MethodSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"""
        RunApp iot-hub\Samples\device\TwinSample "IoTHub\Device\TwinSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"""

        $pnpDeviceSecurityType = "connectionString"
        RunApp iot-hub\Samples\device\PnpDeviceSamples\TemperatureController "IoTHub\Device\PnpDeviceSamples\TemperatureController" "-s $pnpDeviceSecurityType -p ""$env:PNP_TC_DEVICE_CONN_STRING"" -r $sampleRunningTime"
        RunApp iot-hub\Samples\device\PnpDeviceSamples\Thermostat "IoTHub\Device\PnpDeviceSamples\Thermostat" "-s $pnpDeviceSecurityType -p ""$env:PNP_THERMOSTAT_DEVICE_CONN_STRING"" -r $sampleRunningTime"
        # DeviceStreaming sample is not added since it is not available in all regions.

        RunApp iot-hub\Samples\module\ModuleSample "IoTHub\Module\ModuleSample" "-p ""$env:IOTHUB_MODULE_CONN_STRING"" -r $sampleRunningTime"

        RunApp iot-hub\Samples\service\AutomaticDeviceManagementSample "IoTHub\Service\AutomaticDeviceManagementSample"
        RunApp iot-hub\Samples\service\EdgeDeploymentSample "IoTHub\Service\EdgeDeploymentSample"
        RunApp iot-hub\Samples\service\JobsSample "IoTHub\Service\JobsSample"
        RunApp iot-hub\Samples\service\RegistryManagerSample "IoTHub\Service\RegistryManagerSample"

        $deviceId = ($Env:IOTHUB_DEVICE_CONN_STRING.Split(';') | where {$_ -like "DeviceId*"}).Split("=")[1]
        Write-Warning "Using device $deviceId for the ServiceClientSample."
        RunApp iot-hub\Samples\service\ServiceClientSample "IoTHub\Service\ServiceClientSample" "-c ""$env:IOTHUB_CONN_STRING_CSHARP"" -d $deviceId -r $sampleRunningTime"
        # DigitalTwinClientSamples and PnpServiceSamples are not added here since they require the device counterparts to be running as well.

        # TODO #11: Modify Provisioning\device samples to run unattended.

        RunApp provisioning\Samples\service\BulkOperationSample "Provisioning\Service\BulkOperationSample"
        # TODO #11 :RunApp provisioning\Samples\service\EnrollmentGroupSample "Provisioning\Service\EnrollmentGroupSample"
        RunApp provisioning\Samples\service\EnrollmentSample "Provisioning\Service\EnrollmentSample"

        # IoT Hub devices and DPS enrollments cleanup
        RunApp provisioning\Samples\service\CleanupEnrollmentsSample "Provisioning\Service\CleanupEnrollmentsSample"
        RunApp iot-hub\Samples\service\CleanUpDevicesSample "IoTHub\Service\CleanUpDevicesSample"
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
