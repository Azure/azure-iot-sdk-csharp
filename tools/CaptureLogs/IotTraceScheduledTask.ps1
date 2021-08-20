#
# Edit these variables with the values used when copying azcopy and creating the logman traces
#
# If you use the defaults from the readme the ETLLogs and AZCopyLocation will point to the correct locations
#
$ETLLogs = "c:\azcopy\iotlogs"
$AZCopyLocation = "c:\azcopy\azcopy.exe"
$SASToken = "<<your SAS token>>"

# It may be useful to split the folders up into upload times so you can keep the root folder from getting overwhelming
# To do that you would need to add on some identifier to the contiainer.
# If you choose to use this method you must make sure that your SAS Token has CREATE capabilities since you will be adding a new container (folder)
# Also note that depending on how many files you keep on the cleanup you may have duplicates in the folder.

# Example
# $StorageContainerURI = "https://[account].blob.core.windows.net/[container]/" + (Date).ToString("yyyyMMddHHmm")
$StorageContainerURI = "https://[account].blob.core.windows.net/[container]/"

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
# azcopy will not overwrite the log files on the blob store as we've specified the sync command

# If you want to change the lookback period you can do so by changing the CreationTime value. 
# Beware that if you set this value lower than the logman options you could mark the LIVE file for deletion once the new file is created and that could have an impact on logging.
# 
# It is best to keep at minimum (logman time interval)*2 files back so you can ensure that you're not removing the live log file.
# Example, if you are creating a new log file evey hour, you should keep 2 hours of data (preferably more)
# 
# Examples:
# Leave the last 2 hours of logs
# Get-ChildItem $ETLLogs -Filter *.etl | Where-Object {-not $_.PsIsContainer} | Where-Object {$_.CreationTime.TotalMinutes -gt 15} | Remove-Item -Force
#
# Leave the last 15 minutes of logs
# Get-ChildItem $ETLLogs -Filter *.etl | Where-Object {-not $_.PsIsContainer} | Where-Object {$_.CreationTime.TotalMinutes -gt 15} | Remove-Item -Force

Write-Host "Removing previous 24 hours of logs." 
Get-ChildItem $ETLLogs -Filter *.etl | Where-Object {-not $_.PsIsContainer} | Where-Object {$_.CreationTime.TotalHours -gt 24} | Remove-Item -Force
Write-Host "Previous 24 hours of logs removed." 