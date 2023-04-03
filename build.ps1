# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#

.SYNOPSIS
Microsoft Azure IoT SDK .NET build script.

.DESCRIPTION
Builds Azure IoT SDK binaries.

Parameters:
    -configuration {Debug|Release}
    -sign: (Internal use, requires signing toolset) Signs the binaries before release.
    -package: Packs NuGets
    -clean: Runs dotnet clean. Use `git clean -xdf` if this is not sufficient.
    -build: Builds projects (use if re-running tests after a successful build).
    -unittests: Runs unit tests
    -prtests: Runs all tests selected for PR validation at our gates. Requires prerequisites and environment variables.
    -e2etests: Runs the complete E2E test suite. This includes E2E tests, FaultInjection tests and InvalidServiceCertificate tests. Requires prerequisites and environment variables.
    -stresstests: Runs Stress tests.
    -publish: (Internal use, requires nuget toolset) Publishes the nuget packages.
    -verbosity: Sets the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].
    -framework: Select which framework to run tests on. Allowed values examples include, but are not limited to, "netcoreapp3.1", "net47", "net451"
    -skipIotHubTests: Provide this flag if you want to skip all IoT Hub integration tests
    -skipDPSTests: Provide this flag if you want to skip all DPS integration tests
    -runSamples: Ensures all SDK samples build and run with exit code 0.
	

Build will automatically detect if the machine is Windows vs Unix. On Windows development boxes, additional testing on .NET Framework will be performed.

The following environment variables can tune the build behavior:
    - AZURE_IOT_DONOTSIGN: disables delay-signing if set to 'TRUE'

.EXAMPLE
.\build

Builds a Debug version of the SDK.
.EXAMPLE
.\build -configuration Release -build

Builds a Release version of the SDK.
.EXAMPLE
.\build -clean -build -unittests

Builds and runs unit tests (requires prerequisites).
.EXAMPLE
.\build -configuration Release -sign -package -e2etests

Runs E2E tests with already built binaries.
.LINK
https://github.com/azure/azure-iot-sdk-csharp

#>

Param(
    [string] $configuration = "Debug",
    [switch] $sign,
    [switch] $package,
    [switch] $clean,
    [switch] $build,
    [switch] $unittests,
    [switch] $prtests,
    [switch] $e2etests,
    [switch] $stresstests,
    [switch] $publish,
    [string] $verbosity = "q",
    [string] $framework = "*",
    [switch] $skipIotHubTests,
    [switch] $skipDPSTests,
    [switch] $noBuildBeforeTesting,
    [switch] $runAllSamples,
    [switch] $runCleanupSamples
)

Function IsWindows()
{
	return ([Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT)
}

Function CheckSignTools()
{
    $commands = $("SignDotNetBinary", "SignBinary", "SignNuGetPackage", "SignMSIPackage")
    CheckTools $commands
}

Function CheckPublishTools()
{
    $commands = $("PushNuGet")
    CheckTools $commands
}

Function CheckTools($commands)
{
    foreach ($command in $commands)
    {
        $info = Get-Command $command -ErrorAction SilentlyContinue
        if (-not $info)
        {
            throw "Toolset not found: '$command' is missing."
        }
    }
}

Function DidBuildFail($buildOutputFileName)
{
    return Select-String -Path $buildOutputFileName -Pattern 'Build FAILED' -Quiet
}

Function BuildProject($path, $message)
{
    $label = "BUILD: --- $message $configuration ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label

    $projectPath = Join-Path $rootDir $path

    if ($clean)
    {
        & dotnet clean $projectPath --verbosity $verbosity --configuration $configuration
        if ($LASTEXITCODE -ne 0)
        {
            throw "Clean failed: $label"
        }
    }

    & dotnet build $projectPath --verbosity $verbosity --configuration $configuration -warnAsError |  Tee-Object ./buildlog.txt

    if (DidBuildFail "./buildlog.txt" -or $LASTEXITCODE -ne 0)
    {
        throw "Build failed: $label"
    }
}

Function RunSamples($message, $runAll)
{
    $label = "RUNNING SAMPLES: --- $message $configuration ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label

    try 
    {
        Write-Host -ForegroundColor Cyan "Running cleanup samples"

        # Run the cleanup samples so that identities and enrollments created for the samples are cleaned up.
        RunSample 'provisioning\service\samples\getting started\CleanupEnrollmentsSample' "Provisioning\Service\CleanupEnrollmentsSample" "-c ""$env:PROVISIONING_CONNECTION_STRING"""
        RunSample 'iothub\service\samples\how to guides\CleanupDevicesSample' "IoTHub\Service\CleanupDevicesSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -a ""$env:STORAGE_ACCOUNT_CONNECTION_STRING"""
        
        if ($runAll)
        {
            Write-Host -ForegroundColor Cyan "Running all samples"

            $sampleRunningTimeInSeconds = 30

            # Run the iot-hub\device samples
            RunSample 'iothub\device\samples\getting started\FileUploadSample' "IoTHub\Device\FileUploadSample" "-c ""$env:IOTHUB_DEVICE_CONN_STRING"""
            RunSample 'iothub\device\samples\getting started\MessageReceiveSample' "IoTHub\Device\MessageReceiveSample" "-c ""$env:IOTHUB_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
            RunSample 'iothub\device\samples\getting started\MethodSample' "IoTHub\Device\MethodSample" "-c ""$env:IOTHUB_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
            RunSample 'iothub\device\samples\getting started\TwinSample' "IoTHub\Device\TwinSample" "-c ""$env:IOTHUB_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"

            $pnpDeviceSecurityType = "connectionString"
            RunSample iothub\device\samples\solutions\PnpDeviceSamples\TemperatureController "IoTHub\Device\PnpDeviceSamples\TemperatureController" "--DeviceSecurityType $pnpDeviceSecurityType -c ""$env:PNP_TC_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
            RunSample iothub\device\samples\solutions\PnpDeviceSamples\Thermostat "IoTHub\Device\PnpDeviceSamples\Thermostat" "--DeviceSecurityType $pnpDeviceSecurityType -c ""$env:PNP_THERMOSTAT_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
            
            # Run the iot-hub\service samples
            $deviceId = ($Env:IOTHUB_DEVICE_CONN_STRING.Split(';') | Where-Object {$_ -like "DeviceId=*"}).Split("=")[1]
            $iothubHost = ($Env:IOTHUB_CONNECTION_STRING.Split(';') | Where-Object {$_ -like "HostName=*"}).Split("=")[1]
            RunSample 'iothub\service\samples\how to guides\AutomaticDeviceManagementSample' "IoTHub\Service\AutomaticDeviceManagementSample" "-c ""$env:IOTHUB_CONNECTION_STRING"""

            Write-Warning "Using device $deviceId for the AzureSasCredentialAuthenticationSample."
            RunSample 'iothub\service\samples\how to guides\AzureSasCredentialAuthenticationSample' "IoTHub\Service\AzureSasCredentialAuthenticationSample" "-r $iothubHost -d $deviceId -s ""$env:IOTHUB_SAS_KEY"" -n ""$env:IOTHUB_SAS_KEY_NAME"""

            RunSample 'iothub\service\samples\getting started\EdgeDeploymentSample' "IoTHub\Service\EdgeDeploymentSample"
            RunSample 'iothub\service\samples\getting started\JobsSample' "IoTHub\Service\JobsSample"
            RunSample 'iothub\service\samples\how to guides\RegistryManagerSample' "IoTHub\Service\RegistryManagerSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -p ""$env:IOTHUB_X509_DEVICE_PFX_THUMBPRINT"""

            Write-Warning "Using device $deviceId for the RoleBasedAuthenticationSample."
            RunSample 'iothub\service\samples\how to guides\RoleBasedAuthenticationSample' "IoTHub\Service\RoleBasedAuthenticationSample" "-h $iothubHost -d $deviceId --ClientId ""$env:E2E_TEST_AAD_APP_CLIENT_ID"" --TenantId ""$env:MSFT_TENANT_ID"" --ClientSecret ""$env:E2E_TEST_AAD_APP_CLIENT_SECRET"""

            Write-Warning "Using device $deviceId for the ServiceClientSample."
            RunSample 'iothub\service\samples\getting started\ServiceClientSample' "IoTHub\Service\ServiceClientSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -d $deviceId -r $sampleRunningTimeInSeconds"

            # Run provisioning\device samples

            # ComputeDerivedSymmetricKeySample uses the supplied group enrollment key to compute the SHA256 based hash of the supplied device Id.
            # For the sake of running this sample on the pipeline, we will only test the hash computation by passing in a base-64 string and a string to be hashed.
            RunSample 'provisioning\device\samples\getting started\ComputeDerivedSymmetricKeySample' "Provisioning\Device\ComputeDerivedSymmetricKeySample" "-r ""$env:DPS_SYMMETRIC_KEY_INDIVIDUAL_ENROLLMENT_REGISTRATION_ID"" -p ""$env:DPS_SYMMETRIC_KEY_INDIVIDUAL_ENROLLEMNT_PRIMARY_KEY"""

            # Run provisioning\service samples
            RunSample 'provisioning\service\samples\how to guides\BulkOperationSample' "Provisioning\Service\BulkOperationSample" "-c ""$env:PROVISIONING_CONNECTION_STRING"""

        }
    }
    catch [Exception]
    {
        throw "Running Samples Failed: $label"
    }
    finally 
    {
        Set-Location $rootDir
    }
}

Function BuildPackage($path, $message)
{
    $label = "PACK: --- $message $configuration ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label

    $projectPath = Join-Path $rootDir $path
    $projectName = (Get-ChildItem (Join-Path $projectPath *.csproj))[0].BaseName
    Set-Location $projectPath

    if ($sign)
    {
        Write-Host -ForegroundColor Magenta "`tSigning binaries for: $projectName"
        $filesToSign = Get-ChildItem -Path "$projectPath\bin\$configuration\*\$projectName.dll" -Recurse
        SignDotNetBinary $filesToSign
    }

    & dotnet pack --verbosity $verbosity --configuration $configuration --no-build --include-symbols --include-source --output $localPackages

    if ($LASTEXITCODE -ne 0)
    {
        throw "Package failed: $label"
    }

    if ($sign)
    {
        Write-Host -ForegroundColor Magenta "`tSigning package: $projectName"
        $filesToSign = Get-ChildItem (Join-Path $localPackages "$projectName.*.nupkg")
        SignNuGetPackage $filesToSign
    }
}

Function RunTests($message, $framework = "*", $filterTestCategory = "*")
{
    $label = "TEST: --- $message $configuration $framework $filterTestCategory ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label

    $runTestCmd = "dotnet test -s test.runsettings --verbosity $verbosity --configuration $configuration --logger trx --collect 'Code Coverage'"

    if ($noBuildBeforeTesting)
    {
        $runTestCmd += " --no-build"
    }
    if ($filterTestCategory -ne "*")
    {
        $runTestCmd += " --filter '$filterTestCategory'"
    }
    if ($framework -ne "*")
    {
        $runTestCmd += " --framework $framework"
    }

    # By specifying the root dir, the test runner will run all tests in test projects in the VS solution there
    Set-Location $rootDir

    Write-Host "Invoking expression: $runTestCmd ----------"

    Invoke-Expression $runTestCmd

    if ($LASTEXITCODE -ne 0)
    {
        throw "Tests failed: $label"
    }
}

Function RunApp($path, $message, $framework = "netcoreapp3.1")
{
    $label = "RUN: --- $message $configuration ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label
    $appPath = (Join-Path $rootDir $path)

    & dotnet run --project $appPath --framework $framework --verbosity $verbosity --configuration $configuration --logger "trx"

    if ($LASTEXITCODE -ne 0)
    {
        throw "Tests failed: $label"
    }
}

Function RunSample($path, $message, $params)
{
    $label = "RUN: --- $message $configuration ($params)---"

    Write-Host
    Write-Host -ForegroundColor Green $label
    Write-Host "PATH: [$path]"
    Write-Host "MESSAGE: [$message]"
    Write-Host "PARAMS: [$params]"

    Set-Location (Join-Path $rootDir $path)
    Write-Host -ForegroundColor Cyan $label

    $runCommand = "dotnet run -- $params"
    Invoke-Expression $runCommand

    if ($LASTEXITCODE -ne 0)
    {
        throw "Tests failed: $label"
    }
}

$rootDir = (Get-Item -Path ".\" -Verbose).FullName
$localPackages = Join-Path $rootDir "bin\pkg"
$startTime = Get-Date
$buildFailed = $true
$errorMessage = ""

try
{
    if ($sign)
    {
        if ($configuration -ne "Release")
        {
            throw "Do not sign assemblies that aren't release."
        }

        CheckSignTools
    }

    if ($publish)
    {
        CheckPublishTools
    }

    if ($build)
    {
        # SDK binaries
        BuildProject . "Azure IoT .NET SDK Solution"

        # Samples
        # TODO: BuildProject <path> "<desc>"
    }

    if ($runAllSamples)
    {
        RunSamples "Azure IoT .NET SDK Samples" $true
    }

    if ($runCleanupSamples)
    {
        RunSamples "Azure IoT .NET SDK Cleanup Samples" $false
    }

    # Unit Tests require InternalsVisibleTo and can only run in Debug builds.
    if ($unittests)
    {
        if ($configuration -ne "Debug")
        {
            Write-Host -ForegroundColor Magenta "Unit tests must be run in Debug configuration"
        }
        else
        {
            Write-Host
            Write-Host -ForegroundColor Cyan "Unit Test execution"
            Write-Host

            RunTests "Unit tests" -filterTestCategory "TestCategory=Unit" -framework $framework
        }
    }

    if ($prtests)
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "PR validation tests"
        Write-Host

        # Tests categories to include
        $testCategory = "("
        $testCategory += "TestCategory=Unit"
        $testCategory += "|"
        $testCategory += "TestCategory=E2E"
        $testCategory += "|"
        $testCategory += "TestCategory=FaultInjectionBVT"
        $testCategory += ")"

        # test categories to exclude
        $testCategory += "&TestCategory!=LongRunning"
        $testCategory += "&TestCategory!=Flaky"

        # Invalid certificate tests are currently disabled on both Windows and Linux
        # Windows - Invalid cert tests don't currently work with docker on Windows within pipeline agent setup because of virtual host networking configuration issue
        # Linux - The hosted agents are currently referencing a pre-installed newer version of docker (20.10.21+azure-1) which has some compatibility issues with commands
        # that were used with older versions of docker. We're disabling this task until those compatibility issues can be investigated and resolved.

        # Tests categories to exclude
        $testCategory += "&TestCategory!=InvalidServiceCertificate"

        if ($skipIotHubTests)
        {
            $testCategory += "&TestCategory!=IoTHub-Client&TestCategory!=IoTHub-Service"
        }

        if ($skipDPSTests)
        {
            $testCategory += "&TestCategory!=DPS"
        }

        RunTests "PR tests" -filterTestCategory $testCategory -framework $framework
    }

    if ($e2etests)
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "End-to-end Test execution"
        Write-Host

        # Override verbosity to display individual test execution.
        $oldVerbosity = $verbosity
        $verbosity = "normal"

        # Tests categories to include
        $testCategory = "("
        $testCategory += "TestCategory=E2E"
        $testCategory += "|"
        $testCategory += "TestCategory=FaultInjection"
        $testCategory += ")"

        # Invalid certificate tests are currently disabled on both Windows and Linux
        # Windows - Invalid cert tests don't currently work with docker on Windows within pipeline agent setup because of virtual host networking configuration issue
        # Linux - The hosted agents are currently referencing a pre-installed newer version of docker (20.10.21+azure-1) which has some compatibility issues with commands
        # that were used with older versions of docker. We're disabling this task until those compatibility issues can be investigated and resolved.

        # Tests categories to exclude
        $testCategory += "&TestCategory!=InvalidServiceCertificate"

        RunTests "E2E tests" -filterTestCategory $testCategory -framework $framework

        $verbosity = $oldVerbosity
    }

    if ($stresstests)
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "Stress Test execution"
        Write-Host

        RunApp e2e\stress\MemoryLeakTest "MemoryLeakTest test"
    }

    if ($package)
    {
        BuildPackage iothub\device\src "IoT Hub DeviceClient SDK"
        BuildPackage iothub\service\src "IoT Hub ServiceClient SDK"
        BuildPackage provisioning\device\src "Provisioning Device Client SDK"
        BuildPackage provisioning\service\src "Provisioning Service Client SDK"
    }

    if ($publish)
    {
        $files = Get-ChildItem $rootDir\bin\pkg\*.nupkg | Where-Object { -not ($_.Name -match "symbols") }
        $publishResult = PushNuGet $files

        foreach ( $result in $publishResult)
        {
            if ($result.success)
            {
                Write-Host -ForegroundColor Green "OK: $($result.file.FullName)"
            }
            else
            {
                Write-Host -ForegroundColor Red "FAILED: $($result.file.FullName)"
            }
        }
    }

    $buildFailed = $false
}
catch [Exception]
{
    $buildFailed = $true
    $errorMessage = $Error[0]
}
finally
{
    Set-Location $rootDir
    $endTime = Get-Date
}

Write-Host ("`n`nTime Elapsed {0:c}" -f ($endTime - $startTime))

if ($buildFailed)
{
    Write-Host -ForegroundColor Red "Build failed ($errorMessage)"
    exit 1
}
else
{
    Write-Host -ForegroundColor Green "Build succeeded."
    exit 0
}
