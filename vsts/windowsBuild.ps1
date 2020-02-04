# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

Write-Host Start

Write-Host Start ETL logging
logman create trace IotTrace -o iot.etl -pf tools/CaptureLogs/iot_providers.txt
logman start IotTrace

Write-Host Start build
.\vsts\gatedBuild.ps1 -framework $env:FRAMEWORK

if ($LASTEXITCODE -ne 0)
{
    throw "Windows build failed"
}

Write-Host Stop ETL logging
logman stop IotTrace
logman delete IotTrace