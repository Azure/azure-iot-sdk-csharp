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
    Set-Location (Join-Path $rootDir $path)

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
    Set-Location (Join-Path $rootDir $path)

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
    }

    if ($run)
    {
        $sampleRunningTimeInSeconds = 60

        # Run cleanup first so the samples don't get overloaded with old devices
        RunApp provisioning\Samples\service\CleanupEnrollmentsSample "Provisioning\Service\CleanupEnrollmentsSample"
        RunApp iot-hub\Samples\service\CleanupDevicesSample "IoTHub\Service\CleanupDevicesSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -a ""$env:STORAGE_ACCOUNT_CONNECTION_STRING"" --PathToDevicePrefixForDeletion ""$env:PATH_TO_DEVICE_PREFIX_FOR_DELETION_FILE"""

        # Run the iot-hub\device samples
        RunApp iot-hub\Samples\device\DeviceReconnectionSample "IoTHub\Device\DeviceReconnectionSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
        RunApp iot-hub\Samples\device\FileUploadSample "IoTHub\Device\FileUploadSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"""
        RunApp iot-hub\Samples\device\MessageReceiveSample "IoTHub\Device\MessageReceiveSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
        RunApp iot-hub\Samples\device\MethodSample "IoTHub\Device\MethodSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
        RunApp iot-hub\Samples\device\TwinSample "IoTHub\Device\TwinSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"

        $pnpDeviceSecurityType = "connectionString"
        RunApp iot-hub\Samples\device\PnpDeviceSamples\TemperatureController "IoTHub\Device\PnpDeviceSamples\TemperatureController" "--DeviceSecurityType $pnpDeviceSecurityType -p ""$env:PNP_TC_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
        RunApp iot-hub\Samples\device\PnpDeviceSamples\Thermostat "IoTHub\Device\PnpDeviceSamples\Thermostat" "--DeviceSecurityType $pnpDeviceSecurityType -p ""$env:PNP_THERMOSTAT_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
        # DeviceStreaming sample is not added since it is not available in all regions.

        # Run the iot-hub\module sample
        RunApp iot-hub\Samples\module\ModuleSample "IoTHub\Module\ModuleSample" "-p ""$env:IOTHUB_MODULE_CONN_STRING"" -r $sampleRunningTimeInSeconds"

        # Run the iot-hub\service samples
        $deviceId = ($Env:IOTHUB_DEVICE_CONN_STRING.Split(';') | Where-Object {$_ -like "DeviceId=*"}).Split("=")[1]
        $iothubHost = ($Env:IOTHUB_CONNECTION_STRING.Split(';') | Where-Object {$_ -like "HostName=*"}).Split("=")[1]

        RunApp iot-hub\Samples\service\AutomaticDeviceManagementSample "IoTHub\Service\AutomaticDeviceManagementSample"

        Write-Warning "Using device $deviceId for the AzureSasCredentialAuthenticationSample."
        RunApp iot-hub\Samples\service\AzureSasCredentialAuthenticationSample "IoTHub\Service\AzureSasCredentialAuthenticationSample" "-r $iothubHost -d $deviceId -s ""$env:IOT_HUB_SAS_KEY"" -n ""$env:IOT_HUB_SAS_KEY_NAME"""
        
        RunApp iot-hub\Samples\service\EdgeDeploymentSample "IoTHub\Service\EdgeDeploymentSample"
        RunApp iot-hub\Samples\service\JobsSample "IoTHub\Service\JobsSample"
        RunApp iot-hub\Samples\service\RegistryManagerSample "IoTHub\Service\RegistryManagerSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -p ""$env:IOTHUB_X509_DEVICE_PFX_THUMBPRINT"""

        Write-Warning "Using device $deviceId for the RoleBasedAuthenticationSample."
        RunApp iot-hub\Samples\service\RoleBasedAuthenticationSample "IoTHub\Service\RoleBasedAuthenticationSample" "-h $iothubHost -d $deviceId --ClientId ""$env:E2E_TEST_AAD_APP_CLIENT_ID"" --TenantId ""$env:MSFT_TENANT_ID"" --ClientSecret ""$env:E2E_TEST_AAD_APP_CLIENT_SECRET"""

        Write-Warning "Using device $deviceId for the ServiceClientSample."
        RunApp iot-hub\Samples\service\ServiceClientSample "IoTHub\Service\ServiceClientSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -d $deviceId -r $sampleRunningTimeInSeconds"

        # Run provisioning\device samples

        # ComputeDerivedSymmetricKeySample uses the supplied group enrollment key to compute the SHA256 based hash of the supplied device Id.
        # For the sake of running this sample on the pipeline, we will only test the hash computation by passing in a base-64 string and a string to be hashed.
        RunApp provisioning\Samples\device\ComputeDerivedSymmetricKeySample "Provisioning\Device\ComputeDerivedSymmetricKeySample" "-d ""$env:DPS_SYMMETRIC_KEY_INDIVIDUAL_ENROLLMENT_REGISTRATION_ID"" -p ""$env:DPS_SYMMETRIC_KEY_INDIVIDUAL_ENROLLEMNT_PRIMARY_KEY"""
        
        RunApp provisioning\Samples\device\SymmetricKeySample "Provisioning\Device\SymmetricKeySample" "-s ""$env:DPS_IDSCOPE"" -i ""$env:DPS_SYMMETRIC_KEY_INDIVIDUAL_ENROLLMENT_REGISTRATION_ID"" -p ""$env:DPS_SYMMETRIC_KEY_INDIVIDUAL_ENROLLEMNT_PRIMARY_KEY"""

        # Run provisioning\service samples
        RunApp provisioning\Samples\service\BulkOperationSample "Provisioning\Service\BulkOperationSample"
        RunApp provisioning\Samples\service\EnrollmentSample "Provisioning\Service\EnrollmentSample"

        # Run the cleanup again so that identities and enrollments created for the samples are cleaned up.
        RunApp provisioning\Samples\service\CleanupEnrollmentsSample "Provisioning\Service\CleanupEnrollmentsSample"
        RunApp iot-hub\Samples\service\CleanupDevicesSample "IoTHub\Service\CleanupDevicesSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -a ""$env:STORAGE_ACCOUNT_CONNECTION_STRING"" --PathToDevicePrefixForDeletion ""$env:PATH_TO_DEVICE_PREFIX_FOR_DELETION_FILE"""
        RunApp iot-hub\Samples\service\CleanupDevicesSample "IoTHub\Service\CleanupDevicesSample" "-c ""$env:FAR_AWAY_IOTHUB_CONNECTION_STRING"" -a ""$env:STORAGE_ACCOUNT_CONNECTION_STRING"" --PathToDevicePrefixForDeletion ""$env:PATH_TO_DEVICE_PREFIX_FOR_DELETION_FILE"""

        # These samples are currently not added to the pipeilne run. The open items against them need to be addressed before they can be added to the pipeline run.

        # TODO: Not working, for some reason. Need to debug this.
        #RunApp provisioning\Samples\device\TpmSample

        # Tested manually:

        # Ignore: iot-hub\Samples\device\X509DeviceCertWithChainSample - requires the X509 certificate to be placed in the sample execution folder.

        # Ignore: iot-hub\Samples\service\DigitalTwinClientSamples - requires device-side counterpart to run.

        # Ignore: iot-hub\Samples\service\ImportExportDevicesSample - needs to be refactored to accept command-line parameters.
        # This sample also deletes all devices from the referenced hub, so if it is to use our primary hub then this logic will need to be updated.
        
        # Ignore: iot-hub\Samples\service\ImportExportDevicesWithManagedIdentitySample - requires that hubs be set up with managed identity.
        # Also need to ensure that the managed identities are given access to the storage account.

        # Ignore: iot-hub\Samples\service\PnpServiceSamples - requires device-side counterpart to run.

        # Ignore: provisioning\Samples\device\SymmetricKeySample - Group Enrollments requires the derived symmetric key to be computed separately.

        # Ignore: provisioning\Samples\device\X509Sample - requires the X509 certificate to be placed in the sample execution folder.

        # Ignore: provisioning\Samples\service\EnrollmentGroupSample - needs to be refactored to accept command-line parameters.
    }

    $buildFailed = $false
}
catch [Exception]{
    $buildFailed = $true
    $errorMessage = $Error[0]
}
finally {
    Set-Location $rootDir
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