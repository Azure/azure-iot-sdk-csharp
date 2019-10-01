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
    $outputFile = "service$(Get-Date -format "yyyy-MM-dd'T'HH-mm").csv",
    $durationSeconds = 300,
    $type = $null,
    $nowait = $false
)


$fileName = [io.path]::GetFileNameWithoutExtension($outputFile)
$filePath = [io.path]::GetDirectoryName($outputFile)
if ($filePath -eq "") {
    $filePath = pwd
}

if ($type -eq $null)
{
    # Main

    $scriptPath = $PSScriptRoot
    $scriptName = $MyInvocation.MyCommand.Name
    $script = Join-Path $scriptPath $scriptName
    Write-Host "Root: $PSScriptRoot name: $scriptName"

    Write-Host -NoNewline "`t Starting Methods..."
    $proc_method = Start-Process -NoNewWindow powershell -ArgumentList "$script -clients $clients -protocol $protocol -connections $connections -outputFile $outputFile -durationSeconds $durationSeconds -type methods -nowait $nowait" -PassThru
    $handle1 = $proc_method.Handle # Workaround to ensure we have the exit code
    Write-Host "PID $($proc_method.Id)"

    Write-Host -NoNewline "`t Starting C2D..."
    $proc_c2d = Start-Process -NoNewWindow powershell -ArgumentList "$script -clients $clients -protocol $protocol -connections $connections -outputFile $outputFile -durationSeconds $durationSeconds -type c2d -nowait $nowait" -PassThru
    $handle2 = $proc_c2d.Handle # Workaround to ensure we have the exit code
    Write-Host "PID $($proc_c2d.Id)"

    Write-Host -NoNewline "`t Waiting for processes to finish..."
    Wait-Process -Id ($proc_method.Id, $proc_c2d.Id)
    Write-Host "Done"

    $err = 0
    if ($proc_method.ExitCode -ne 0)
    {
        Write-Error "ServiceClient Methods failed with exit code: $($proc_method.ExitCode)"
        $err = $proc_method.ExitCode 
    }

    if ($proc_c2d.ExitCode -ne 0)
    {
        Write-Error "ServiceClient Methods failed with exit code: $($proc_c2d.ExitCode)"
        $err = $proc_c2d.ExitCode
    }
    
    if ($err -ne 0)
    {
        foreach ($file in (ls *.err))
        {
            Write-Host -ForegroundColor Red "ERRORS $file"
            cat $file
            Write-Host
        }
    }

    rm -ErrorAction Continue *.err

    exit $err #One of the methods or c2d error codes
}
elseif ($type -eq "methods")
{
    # Methods Fork

    $host.ui.RawUI.WindowTitle = "Azure IoT SDK: Service Stress [Methods]"
    Write-Host -ForegroundColor Cyan "`nSERVICE: Methods`n"

    $scenario = "service_method"
    $out = Join-Path $filePath "$fileName.$($scenario).csv"
    if (Test-Path $out) 
    {
        rm $out
    }
    
}
elseif ($type -eq "c2d")
{
    # C2D Fork

    $host.ui.RawUI.WindowTitle = "Azure IoT SDK: Service Stress [C2D]"
    Write-Host -ForegroundColor Cyan "`nSERVICE: C2D`n"
    $scenario = "service_c2d"
    $out = Join-Path $filePath "$fileName.$($scenario).csv"

    if (Test-Path $out) 
    {
        rm $out
    }
}
else
{
    Write-Error "Unknown test type $type".
}

# Fork (C2D/Methods)
$proc_sevice = Start-Process -NoNewWindow dotnet -ArgumentList "run --no-build -c Release -- -t $durationSeconds -o $out -p $protocol -n $clients -c $connections -f $scenario -s 2048" -PassThru -RedirectStandardError "$out.err"
$handle3 = $proc_sevice.Handle # Workaround to ensure we have the exit code
Wait-Process -Id $proc_sevice.Id

if ($proc_sevice.ExitCode -ne 0)
{
    Write-Error "ServiceClient failed with exit code: $($proc_sevice.ExitCode)"
    $err = $proc_sevice.ExitCode 

    Write-Error "ERRORS:"
    cat "$out.err"
}

if (-not $nowait) 
{
    Read-Host "Press ENTER to close"
}

exit $proc_sevice.ExitCode
