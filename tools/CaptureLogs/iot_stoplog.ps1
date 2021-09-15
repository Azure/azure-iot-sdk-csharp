param(
    [Parameter(Mandatory)]
    [string] $TraceName
)

Function StopLogCapture()
{
    $stopTrace = "logman stop $TraceName"
    Write-Host "Invoking: $stopTrace."
    Invoke-Expression $stopTrace

    $deleteTrace = "logman delete $TraceName"
    Write-Host "Invoking: $deleteTrace."
    Invoke-Expression $deleteTrace
}

StopLogCapture