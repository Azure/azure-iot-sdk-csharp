# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#

.SYNOPSIS
Microsoft Azure IoT SDK .NET version update script.

.DESCRIPTION
Updates the versions of the Azure IoT SDK components.

Parameters:
    -verify Verifies if the versions correspond with the ones in versions.csv.
    -update Updates the project versions with the ones in versions.csv.

.EXAMPLE
.\build.ps1

.NOTES
This tool changes the CSPROJ files.

.LINK
https://github.com/azure/azure-iot-sdk-csharp

#>

Param(
    [switch] $update
)

Function GetVersion($path) {
    $extension = Split-Path -leaf $path
    
    $x = [xml](Get-Content $path)
    $versionNode = Select-Xml "//Version" $x
    return $versionNode
}

Function UpdateVersion($path, $currentVersion, $desiredVersion) {
    $actual = "<Version>$currentVersion</Version>"
    $desired = "<Version>$desiredVersion</Version>"
    (Get-Content $path) -replace $actual, $desired | Set-Content -Encoding UTF8 $path
}

$csv = Import-Csv versions.csv

$requireUpdate = 0

foreach ($project in $csv) {

    Write-Host -ForegroundColor Cyan (Split-Path -leaf $project.AssemblyPath)
    $desiredVersion = $project.Version
    $actualVersionNode = GetVersion($project.AssemblyPath)
    $actualVersion = $actualVersionNode.Node.InnerText

    if ($actualVersion -ne $desiredVersion)
    {
        $requireUpdate++
       
        if ($update)
        {
            Write-Host -Foreground Green "`tVersion: $actualVersion Desired: $desiredVersion"
            UpdateVersion $project.AssemblyPath $actualVersion $desiredVersion
        }
        else
        {
            Write-Host -Foreground Red "`tVersion: $actualVersion Desired: $desiredVersion"
        }
    }
    else
    {
        Write-Host "`tVersion: $actualVersion"
    }
}

if (-not $update -and $requireUpdate)
{
    Write-Host "Run 'versionupdate.ps1 -update' to apply these version changes to projects."
}

exit $requireUpdate
