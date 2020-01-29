Write-Host Start

Write-Host Start ETL logging
logman create trace IotTrace -o iot.etl -pf tools/CaptureLogs/iot_providers.txt
logman start IotTrace

Write-Host Start build
.\gatedBuild.ps1

Write-Host Stop ETL logging
logman stop IotTrace
logman delete IotTrace