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
    -prtests: Runs all tests selected for PR validation
    -e2etests: Runs E2E tests. Requires prerequisites and environment variables.
    -stresstests: Runs Stress tests.
    -publish: (Internal use, requires nuget toolset) Publishes the nuget packages.
    -verbosity: Sets the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].
    -framework: Select which framework to run tests on. Allowed values examples include, but are not limited to, "netcoreapp3.1", "net47", "net451"
    -skipIotHubTests: Provide this flag if you want to skip all IoT Hub integration tests
    -skipDPSTests: Provide this flag if you want to skip all DPS integration tests
	

Build will automatically detect if the machine is Windows vs Unix. On Windows development boxes, additional testing on .NET Framework will be performed.

The following environment variables can tune the build behavior:
    - AZURE_IOT_DONOTSIGN: disables delay-signing if set to 'TRUE'
    - AZURE_IOT_LOCALPACKAGES: the path to the local nuget source. 
        Add a new source using: `nuget sources add -name MySource -Source <path>`
        Remove a source using: `nuget sources remove -name MySource`

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
    [switch] $noBuildBeforeTesting
)

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

Function CheckLocalPackagesAvailableForTesting()
{
    return (-not [string]::IsNullOrWhiteSpace($env:AZURE_IOT_LOCALPACKAGES))
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

Function BuildPackage($path, $message)
{
    $label = "PACK: --- $message $configuration ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label

    $projectPath = Join-Path $rootDir $path
    $projectName = (Get-ChildItem (Join-Path $projectPath *.csproj))[0].BaseName

    if ($sign)
    {
        Set-Location $projectPath
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

    $runTestCmd = "dotnet test -s test.runsettings --verbosity $verbosity --configuration $configuration --logger trx"

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

    # Ensure the E2E tests can log to Application Insights
    if ([string]::IsNullOrWhiteSpace($env:E2E_IKEY))
    {
        throw "Application Insights is not configured for logging in E2E tests."
    }

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

$rootDir = (Get-Item -Path ".\" -Verbose).FullName
$localPackages = Join-Path $rootDir "bin\pkg"
$startTime = Get-Date
$buildFailed = $true
$errorMessage = ""
$localPackagesAvailableForTesting = CheckLocalPackagesAvailableForTesting

Write-Host -ForegroundColor Magenta "Local packages being used for testing: $localPackagesAvailableForTesting"

try
{
    if ($sign)
    {
        if (-not $localPackagesAvailableForTesting)
        {
            throw "Local NuGet package source path is not set, required when signing packages."
        }

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
        # We must disable package testing here as the E2E csproj may reference new APIs that are not available in existing NuGet packages.
        $packageTempPath = $env:AZURE_IOT_LOCALPACKAGES
        $env:AZURE_IOT_LOCALPACKAGES = ""
        # SDK binaries
        BuildProject . "Azure IoT C# SDK Solution"
        $env:AZURE_IOT_LOCALPACKAGES = $packageTempPath
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
        $testCategory += ")"

        # test categories to exclude
        $testCategory += "&TestCategory!=LongRunning"
        $testCategory += "&TestCategory!=FaultInjection"
        $testCategory += "&TestCategory!=Flaky"

        if ($skipIotHubTests)
        {
            $testCategory += "&TestCategory!=IoTHub"
        }

        if ($skipDPSTests)
        {
            $testCategory += "&TestCategory!=DPS"
        }

        RunTests "PR tests" -filterTestCategory $testCategory -framework $framework
    }

    if ($package)
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

    if ($localPackagesAvailableForTesting)
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "Preparing local package source"
        Write-Host

        if (-not (Test-Path $env:AZURE_IOT_LOCALPACKAGES))
        {
            throw "Local NuGet package source path invalid: $($env:AZURE_IOT_LOCALPACKAGES)"
        }

        Write-Host Following local packages found:
        Get-ChildItem -Path $env:AZURE_IOT_LOCALPACKAGES
    }

    if ($e2etests)
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "End-to-end Test execution"
        Write-Host

        if ($localPackagesAvailableForTesting)
        {
            Write-Host -ForegroundColor Magenta "IMPORTANT: Using local packages."
        }

        # Tests categories to include
        $testCategory = "("
        $testCategory += "TestCategory=E2E"
        $testCategory += "|"
        $testCategory += "TestCategory=InvalidServiceCertificate"
        $testCategory += ")"


        # Override verbosity to display individual test execution.
        $oldVerbosity = $verbosity
        $verbosity = "normal"

        RunTests "E2E tests" -filterTestCategory $testCategory -framework $framework

        $verbosity = $oldVerbosity

        # Samples
        BuildProject security\tpm\samples "SecurityProvider for TPM Samples"
    }

    if ($stresstests)
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "Stress Test execution"
        Write-Host

        RunApp e2e\stress\MemoryLeakTest "MemoryLeakTest test"
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
