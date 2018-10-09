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
    -nopackage: Skips NuGet packaging
    -e2etests: Runs E2E tests. Requires prerequisites and environment variables.
    -stresstests: Runs Stress tests.
    -xamarintests: Runs Xamarin tests. Requires additional SDKs and prerequisite configuration.
    -sign: (Internal use, requires signing toolset) Signs the binaries before release.
    -publish: (Internal use, requires nuget toolset) Publishes the nuget packages.
    -configuration {Debug|Release}
    -verbosity: Sets the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].

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
    [switch] $sign,
    [switch] $publish,
    [string] $configuration = "Debug",
    [string] $verbosity = "q"
)

Function IsWindowsDevelopmentBox()
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
    foreach($command in $commands)
    {
        $info = Get-Command $command -ErrorAction SilentlyContinue
        if ($info -eq $null)
        {
            throw "Toolset not found: '$command' is missing."
        }
    }
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

Function BuildPackage($path, $message) {
    $label = "PACK: --- $message $configuration ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label

    $projectPath = Join-Path $rootDir $path
    cd $projectPath

    $projectName = (dir (Join-Path $projectPath *.csproj))[0].BaseName

    if ($sign)
    {
        Write-Host -ForegroundColor Magenta "`tSigning binaries: $projectName"
        $filesToSign = dir -Recurse .\bin\Release\$projectName.dll
        SignDotNetBinary $filesToSign
    }

    & dotnet pack --verbosity $verbosity --configuration $configuration --no-build --include-symbols --include-source --output $localPackages

    if ($LASTEXITCODE -ne 0) {
        throw "Package failed: $label"
    }

    if ($sign)
    {
        Write-Host -ForegroundColor Magenta "`tSigning package: $projectName"
        $filesToSign = dir (Join-Path $localPackages "$projectName.*.nupkg")
        SignNuGetPackage $filesToSign
    }
}

Function RunTests($path, $message, $framework="netcoreapp2.1") {

    $label = "TEST: --- $message $configuration ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label
    cd (Join-Path $rootDir $path)

    & dotnet test --framework $framework --verbosity $verbosity --configuration $configuration --logger "trx"

    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed: $label"
    }
}

Function RunApp($path, $message, $framework="netcoreapp2.1") {

    $label = "RUN: --- $message $configuration ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label
    cd (Join-Path $rootDir $path)

    & dotnet run --framework $framework --verbosity $verbosity --configuration $configuration --logger "trx"

    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed: $label"
    }
}

$rootDir = (Get-Item -Path ".\" -Verbose).FullName
$localPackages = Join-Path $rootDir "bin\pkg"
$startTime = Get-Date
$buildFailed = $true
$errorMessage = ""

try {
    if ($sign)
    {
        if ([string]::IsNullOrWhiteSpace($env:AZURE_IOT_LOCALPACKAGES))
        {
            throw "Local NuGet package source path is not set, required when signing packages."
        }

        CheckSignTools
    }

    if ($publish)
    {
        CheckPublishTools
    }

    if (-not $nobuild)
    {
        # SDK binaries
        BuildProject . "Azure IoT C# SDK Solution"
    }

    # Unit Tests require InternalsVisibleTo and can only run in Debug builds.
    if ((-not $nounittests) -and ($configuration.ToUpperInvariant() -eq "DEBUG"))
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

    if (-not [string]::IsNullOrWhiteSpace($env:AZURE_IOT_LOCALPACKAGES))
    {
        Write-Host
        Write-Host -ForegroundColor Cyan "Preparing local package source"
        Write-Host

        if (-not (Test-Path $env:AZURE_IOT_LOCALPACKAGES))
        {
            throw "Local NuGet package source path invalid: $($env:AZURE_IOT_LOCALPACKAGES)"
        }

        # Clear the NuGet cache and the old packages.
        dotnet nuget locals --clear all
        Remove-Item $env:AZURE_IOT_LOCALPACKAGES\*.*

        # Copy new packages.
        copy (Join-Path $rootDir "bin\pkg\*.*") $env:AZURE_IOT_LOCALPACKAGES
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
            RunTests e2e\test "End-to-end tests (NET47)" "net47"
            RunTests e2e\test "End-to-end tests (NET451)" "net451"
        }

        $verbosity = $oldVerbosity

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

    if ($publish)
    {
        $files = dir $rootDir\bin\pkg\*.nupkg | where {-not ($_.Name -match "symbols")}
        $publishResult = PushNuGet $files

        foreach( $result in $publishResult)
        {
            if($result.success)
            {
                Write-Host -ForegroundColor Green "OK    : $($result.file.FullName)"
            }
            else
            {
                Write-Host -ForegroundColor Red "FAILED: $($result.file.FullName)"
            }
        }
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
