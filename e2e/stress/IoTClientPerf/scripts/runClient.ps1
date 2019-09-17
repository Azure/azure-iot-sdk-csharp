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
    $outputFile = "device$(Get-Date -format "yyyy-MM-dd'T'HH-mm").csv",
    $durationSeconds = 300,
    $scenario = "device_all",
    [switch] $fault = $false,
    $faultStartDelaySeconds = 60,
    $faultDurationSeconds = 30
)

function Test-Administrator  
{  
    $user = [Security.Principal.WindowsIdentity]::GetCurrent();
    (New-Object Security.Principal.WindowsPrincipal $user).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)  
}

$host.ui.RawUI.WindowTitle = "Azure IoT SDK: Device Stress"

$fileName = [io.path]::GetFileNameWithoutExtension($outputFile)
$filePath = [io.path]::GetDirectoryName($outputFile)
if ($filePath -eq "") {
    $filePath = pwd
}

if ($fault -and (-not (Test-Administrator)))
{
    Write-Error "Fault injection requires administrator rights. Run elevated or without -fault"
    exit 1
}

Write-Host -ForegroundColor Cyan "`nDEVICE scenario: $scenario`n"
$out = Join-Path $filePath "$fileName.$($scenario).csv"

$proc_device = Start-Process -NoNewWindow dotnet -ArgumentList "run --no-build -c Release -- -t $durationSeconds -o $out -p $protocol -n $clients -c $connections -f $scenario -s 2048" -PassThru
$handle = $proc_device.Handle # Workaround to ensure we have the exit code

if ($fault)
{
    $hubHostName = $Env:IOTHUB_CONN_STRING_CSHARP.Split(';')[0].Split('=')[1]
    $scriptPath = $PSScriptRoot
    
    Write-Host -ForegroundColor Magenta "Fault requested after $faultStartDelaySeconds for $hubHostName"
    Start-Sleep $faultStartDelaySeconds
    $proc_fault = Start-Process -NoNewWindow powershell -ArgumentList "$(Join-Path $scriptPath 'blockPortToHub') -IotHubHostName $hubHostName -BlockDurationSeconds $faultDurationSeconds" -PassThru
    $handle2 = $proc_fault.Handle # Workaround to ensure we have the exit code
}

Wait-Process $proc_device.Id

$err = 0
if ($proc_device.ExitCode -ne 0)
{
    Write-Error "DeviceClient failed with exit code: $($proc_device.ExitCode)"
    $err = $proc_device.ExitCode 
}


if ($fault -and ($proc_fault.ExitCode -ne 0))
{
    Write-Error "FaultInjection failed with exit code: $($proc_fault.ExitCode)"
    $err = $proc_fault.ExitCode    
}

exit $err
