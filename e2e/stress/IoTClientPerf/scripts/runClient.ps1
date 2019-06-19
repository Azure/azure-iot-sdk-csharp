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


Write-Host -ForegroundColor Cyan "`nDEVICE: C2D`n"
& dotnet run --no-build -c Release -- -t $durationSeconds -o $outputFile -p $protocol -n $clients -c $connections -f device_c2d


Write-Host -ForegroundColor Cyan  "`nDEVICE: Methods`n"
& dotnet run --no-build -c Release -- -t $durationSeconds -o $outputFile -p $protocol -n $clients -c $connections -f device_method


Write-Host -ForegroundColor Cyan "`nDEVICE: All`n"
& dotnet run --no-build -c Release -- -t $durationSeconds -o $outputFile -p $protocol -n $clients -c $connections -f device_all


Write-Host -ForegroundColor Cyan "`nDEVICE: D2C`n"
& dotnet run --no-build -c Release -- -t $durationSeconds -o $outputFile -p $protocol -n $clients -c $connections -f device_d2c
