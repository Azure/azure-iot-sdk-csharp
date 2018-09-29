Function Invoke-Git
{
    & git $Args
    if ($LASTEXITCODE) 
    {
        throw "Git error: $LASTEXITCODE"
    }
}

if ([string]::IsNullOrWhiteSpace($env:COMMIT_FROM))
{
    Write-Host -ForegroundColor Cyan "COMMIT_FROM empty - preserving VSTS repository state:"
    Invoke-Git status -vv
    Invoke-Git log -1
    return
}

$pr = $false
if ($env:COMMIT_FROM -match "^[\d]+$") 
{
    Write-Host -ForegroundColor Cyan "Verifying PR #$($env:COMMIT_FROM)"
    $pr = $true
}
else
{
    Write-Host -ForegroundColor Cyan "Verifying BRANCH $($env:COMMIT_FROM)"
}

if ($pr)
{
    Write-Host -ForegroundColor Cyan "Fetching PR #$($env:COMMIT_FROM)"
    Invoke-Git fetch origin pull/$env:COMMIT_FROM/head:pr$env:COMMIT_FROM
    Invoke-Git checkout pr$env:COMMIT_FROM
    Invoke-Git status
}
else
{
    Write-Host -ForegroundColor Cyan "Fetching BRANCH $($env:COMMIT_FROM)"
    Invoke-Git fetch origin $env:COMMIT_FROM:$env:COMMIT_FROM
    Invoke-Git checkout $env:COMMIT_FROM
    Invoke-Git status
}

Write-Host -ForegroundColor Cyan "Last 5 from log:"
Invoke-Git log -5
