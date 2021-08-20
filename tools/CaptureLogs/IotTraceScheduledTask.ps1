$ETLLogs = "<<PATH TO ETL LOGS>>; ex. c:\perflogs\iot"
$AZCopyLocation = "<<PATH TO AZ COPY>>; ex. c:\azcopy\azcopy.exe"
$SASToken = "<<YOUR SAS TOKEN>>"
$StorageContainerURI = "https://[account].blob.core.windows.net/[container]/[path/to/directory]"

$combinedURI = "$StorageContainerURI?$SASToken"

$azcopy = Get-Command $AZCopyLocation -ErrorAction SilentlyContinue
if ($null -eq $azcopy)
{
    Write-Host -ForegroundColor Red "azcopy was not found. Exiting script." 
    exit
}

$return = $null
if (-not [System.Uri]::TryCreate(($combinedURI, "RelativeOrAbsolute", [ref] $return))
{
    Write-Host -ForegroundColor Red "The combined Azure Storage URI was invalid. Please check the StorageContainerURI variable and the SASToken variable. Exiting script." 
    exit
}

Write-Host "Starting azcopy." 
$azcopySwitches = "copy", "$ETLLogs", "$combinedURI", "--include-pattern", "*.etl", "--overwrite", "ifSourceNewer"
& $azcopy $azcopySwitches
Write-Host "azcopy completed." 

# Keep the last 24 hours of logs
# azcopy will not overwrite the log files on the blob store as we've specified the ifSourceNewer option
Write-Host "Removing previous 24 hours of logs." 
Get-ChildItem $ETLLogs -Filter *.etl | where {-not $_.PsIsContainer} | where {$_.CreationTime.TotalHours -gt 24} | Remove-Item -Force
Write-Host "Previous 24 hours of logs removed." 