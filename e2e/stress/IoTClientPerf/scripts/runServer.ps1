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
    $durationSeconds = 1800
)


$fileName = [io.path]::GetFileNameWithoutExtension($outputFile)

Write-Host "SERVICE: Methods"
Start-Process dotnet -ArgumentList 'run', '--no-build', '-c', 'Release', '--', '-t', $durationSeconds, '-o', "$fileName.method.csv", '-p', $protocol, '-n', $clients, '-c', $connections, '-f', 'service_method'

Write-Host "SERVICE: C2D"
Start-Process dotnet -ArgumentList 'run', '--no-build', '-c', 'Release', '--', '-t', $durationSeconds, '-o', "$fileName.c2d.csv", '-p', $protocol, '-n', $clients, '-c', $connections, '-f', 'service_c2d'

Write-Host "Services started in separate consoles."
