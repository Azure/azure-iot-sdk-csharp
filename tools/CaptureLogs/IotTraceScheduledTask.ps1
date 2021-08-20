$ETLLogs = "c:\perflogs\iot"
$AZCopyLocation = "<<PATH TO AZ COPY>>"
$SASToken = "<<YOUR SAS TOKEN>>"
$StorageContainerURI = "https://[account].blob.core.windows.net/[container]/[path/to/directory]"

$combinedURI = "$StorageContainerURI?$SASToken"

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
{
    Write-Host -ForegroundColor Red "You must run this script as an elevated user. Exiting script." 
    exit
}

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
$azcopySwitches = "copy", "$ETLLogs", "$StorageContainerURI?$SASToken", "--include-pattern", "*.etl", "--overwrite", "ifSourceNewer"
& $azcopy $azcopySwitches
Write-Host "azcopy completed." 

# Keep the last 24 hours of logs
# azcopy will not overwrite the log files on the blob store as we've specified the ifSourceNewer option
Write-Host "Removing previous 24 hours of logs." 
Get-ChildItem $ETLLogs -Filter *.etl | where {-not $_.PsIsContainer} | where {$_.CreationTime.TotalHours -gt 24} | Remove-Item -Force
Write-Host "Previous 24 hours of logs removed." 