# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#

.SYNOPSIS
Microsoft Azure IoT SDK .NET Stress Test script.

.DESCRIPTION
Runs the client portion of the test.

.EXAMPLE
./runServer

#>

Param(
    $clients = 100,
    $protocol = "amqp",
    $connections = 10,
    $outputFile = "service.csv",
    $durationSeconds = 1800,
    $type = $null
)


$fileName = [io.path]::GetFileNameWithoutExtension($outputFile)
$filePath = [io.path]::GetDirectoryName($outputFile)

if ($type -eq $null)
{
    $scriptPath = $PSScriptRoot
    $scriptName = $MyInvocation.MyCommand.Name
    $script = Join-Path $scriptPath $scriptName
    Write-Host "Root: $PSScriptRoot name: $scriptName"

    Start-Process powershell -ArgumentList "$script -clients $clients -protocol $protocol -connections $connections -outputFile $outputFile -durationSeconds $durationSeconds -type methods"
    Start-Process powershell -ArgumentList "$script -clients $clients -protocol $protocol -connections $connections -outputFile $outputFile -durationSeconds $durationSeconds -type c2d"

    exit
}
elseif ($type -eq "methods")
{
    $host.ui.RawUI.WindowTitle = "Azure IoT SDK: Service Stress [Methods]"
    Write-Host -ForegroundColor Cyan "`nSERVICE: Methods`n"

    $out = Join-Path $filePath "$fileName.method.csv"
    $scenario = "service_method"
}
elseif ($type -eq "c2d")
{
    $host.ui.RawUI.WindowTitle = "Azure IoT SDK: Service Stress [C2D]"
    Write-Host -ForegroundColor Cyan "`nSERVICE: C2D`n"
    $out = Join-Path $filePath "$fileName.c2d.csv"
    $scenario = "service_c2d"
}
else
{
    Write-Error "Unknown test type $type".
}


& dotnet run --no-build -c Release -- -t $durationSeconds -o $out -p $protocol -n $clients -c $connections -f $scenario
Read-Host "Press ENTER to close"
