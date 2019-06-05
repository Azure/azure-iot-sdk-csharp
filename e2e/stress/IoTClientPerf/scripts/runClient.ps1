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


Write-Host "DEVICE: C2D"
& dotnet run --no-build -c Release -- -t $durationSeconds -o $outputFile -p $protocol -n $clients -c $connections -f device_c2d


Write-Host "DEVICE: Methods"
& dotnet run --no-build -c Release -- -t $durationSeconds -o $outputFile -p $protocol -n $clients -c $connections -f device_method


Write-Host "DEVICE: All"
& dotnet run --no-build -c Release -- -t $durationSeconds -o $outputFile -p $protocol -n $clients -c $connections -f device_all


Write-Host "DEVICE: D2C"
& dotnet run --no-build -c Release -- -t $durationSeconds -o $outputFile -p $protocol -n $clients -c $connections -f device_d2c
