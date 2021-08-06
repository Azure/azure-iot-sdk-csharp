param(
    [Parameter(Mandatory)]
    [string] $TraceName,

    [Parameter(Mandatory)]
    [string] $Output,

    [Parameter(Mandatory)]
    [string] $ProviderFile,
    
    [Parameter()]
    [ValidateSet('bin','bincirc')]
    [string] $Format,

    [Parameter()]
    [ValidateRange(1, [int]::MaxValue)]
    [int] $MaximumOutputSize
)

Function StartLogCapture()
{
    $createTrace = "logman create trace $TraceName -o $Output -pf $ProviderFile"

    if ($Format.Length -gt 0)
    {
        $createTrace += " -f $Format"
    }
    if ($MaximumOutputSize -gt 0)
    {
        $createTrace += " -max $MaximumOutputSize"
    }

    Write-Host "Invoking: $createTrace."
    Invoke-Expression $createTrace

    $startTrace = "logman start $TraceName"
    Write-Host "Invoking: $startTrace."
    Invoke-Expression $startTrace
}

StartLogCapture