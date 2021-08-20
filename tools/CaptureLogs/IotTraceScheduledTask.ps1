$ETLLogs = "c:\perflogs\iot"
$AZCopyLocation = "c:\azcopy\azcopy.exe"
$SASToken = "<<your SAS token>>"
$StorageContainerURI = "https://[account].blob.core.windows.net/[container]"

# Optionally you can replace this variable with a pre combined SAS token URL
$CombinedURI = "$StorageContainerURI`?$SASToken"
# $CombinedURI = "https://[account].blob.core.windows.net/[container]?[SAS]"

# Validate azcopy is present
$azcopy = Get-Command $AZCopyLocation -ErrorAction SilentlyContinue
if ($null -eq $azcopy)
{
    Write-Host -ForegroundColor Red "azcopy was not found. Exiting script." 
    exit
}

# Validate the SAS token URL is correctly formatted
$uriCheck = $null
if (-not [System.Uri]::TryCreate($CombinedURI, "RelativeOrAbsolute", [ref] $uriCheck))
{
    Write-Host -ForegroundColor Red "The combined Azure Storage URI was invalid. Please check the StorageContainerURI variable and the SASToken variable. Exiting script." 
    exit
}

# Make sure we have access to the ETL Logs location
if (-not (Test-Path $ETLLogs))
{
    Write-Host -ForegroundColor Red "The ETL logs path is invalid. Please check the ETLLogs variable." 
    exit
}

Write-Host "Starting azcopy." 
$azcopySwitches = "copy", "$ETLLogs", "$CombinedURI", "--include-pattern", "*.etl", "--overwrite", "ifSourceNewer", "--recursive"
& $azcopy $azcopySwitches
Write-Host "azcopy completed." 

# Keep the last 24 hours of logs
# azcopy will not overwrite the log files on the blob store as we've specified the ifSourceNewer option
Write-Host "Removing previous 24 hours of logs." 
Get-ChildItem $ETLLogs -Filter *.etl | where {-not $_.PsIsContainer} | where {$_.CreationTime.TotalHours -gt 24} | Remove-Item -Force
Write-Host "Previous 24 hours of logs removed." 