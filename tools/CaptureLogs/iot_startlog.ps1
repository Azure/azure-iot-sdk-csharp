param(
    [Parameter(Mandatory)]
    [string] $TraceName,

    [Parameter(Mandatory)]
    [string] $Output,

    [Parameter(Mandatory)]
    [string] $ProviderFile
)

Function StartLogCapture()
{
    $createTrace = "logman create trace $TraceName -o $Output -pf $ProviderFile"
    Write-Host "Invoking: $createTrace."
    Invoke-Expression $createTrace

    $startTrace = "logman start $TraceName"
    Write-Host "Invoking: $startTrace."
    Invoke-Expression $startTrace
}

StartLogCapture