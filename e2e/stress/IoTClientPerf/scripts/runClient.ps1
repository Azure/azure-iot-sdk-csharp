# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#

.SYNOPSIS
Microsoft Azure IoT SDK .NET Stress Test script.

.DESCRIPTION
Runs the client portion of the test.

.EXAMPLE
./runClient

#>

Param(
    $clients = 100,
    $protocol = "amqp",
    $connections = 10,
    $outputFile = "client.csv",
    $durationSeconds = 300
)

$host.ui.RawUI.WindowTitle = "Azure IoT SDK: Device Stress"

$fileName = [io.path]::GetFileNameWithoutExtension($outputFile)
$filePath = [io.path]::GetDirectoryName($outputFile)
if ($filePath -eq "") {
    $filePath = pwd
}

Write-Host -ForegroundColor Cyan "`nDEVICE: C2D`n"
$scenario = "device_c2d"
$out = Join-Path $filePath "$fileName.$($scenario).csv"
& dotnet run --no-build -c Release -- -t $durationSeconds -o $out -p $protocol -n $clients -c $connections -f $scenario -s 2048


Write-Host -ForegroundColor Cyan  "`nDEVICE: Methods`n"
$scenario = "device_method"
$out = Join-Path $filePath "$fileName.$($scenario).csv"
& dotnet run --no-build -c Release -- -t $durationSeconds -o $out -p $protocol -n $clients -c $connections -f $scenario -s 2048

Write-Host -ForegroundColor Cyan "`nDEVICE: All`n"
$scenario = "device_all"
$out = Join-Path $filePath "$fileName.$($scenario).csv"

& dotnet run --no-build -c Release -- -t $durationSeconds -o $out -p $protocol -n $clients -c $connections -f $scenario -s 2048


Write-Host -ForegroundColor Cyan "`nDEVICE: D2C`n"
$scenario = "device_d2c"
$out = Join-Path $filePath "$fileName.$($scenario).csv"
& dotnet run --no-build -c Release -- -t $durationSeconds -o $out -p $protocol -n $clients -c $connections -f $scenario -s 2048
