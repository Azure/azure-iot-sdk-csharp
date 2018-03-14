# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#

.SYNOPSIS
DeviceExplorer build script.

.DESCRIPTION
Builds DeviceExplorer.

Parameters:
    -clean: Runs dotnet clean. Use `git clean -xdf` if this is not sufficient.
    -configuration {Debug|Release}

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
    [switch] $sign,
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

    foreach($command in $commands)
    {
        $info = Get-Command $command -ErrorAction SilentlyContinue
        if ($info -eq $null)
        {
            throw "Sign toolset not found: '$command' is missing."
        }
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

Function BuildMSI() {

    Write-Host
    Write-Host -ForegroundColor Cyan "MSI: --- DeviceExplorer MSI ---"
    cd (Join-Path $rootDir $path)

    $commandLine = "devenv .\DeviceExplorerWithInstaller.sln /project SetupDeviceExplorer /build `"$configuration|Any CPU`""
    
    cmd /c "$commandLine && exit /b !ERRORLEVEL!"

    if ($LASTEXITCODE -ne 0) {
        throw "MSI packaging failed."
    }
}

$rootDir = (Get-Item -Path ".\" -Verbose).FullName
$startTime = Get-Date
$buildFailed = $true
$errorMessage = ""

try {

    if (-not (IsWindowsDevelopmentBox))
    {
        throw "Building DeviceExplorer requires a Windows development box and Visual Studio."
    }

    if ($sign)
    {
        CheckSignTools
    }
    
    LegacyBuildProject build "IoT Hub Device Explorer"
    
    if ($sign)
    {
        $files = dir .\DeviceExplorer\bin\Debug\DeviceExplorer.exe
        SignBinary $files
    }

    BuildMSI

    if ($sign)
    {
        
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
