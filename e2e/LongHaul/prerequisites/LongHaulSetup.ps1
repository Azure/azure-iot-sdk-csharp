# NOTE: This script needs to be run using admin mode

param(
    [Parameter(Mandatory)]
    [string] $Region,

    [Parameter(Mandatory)]
    [string] $ResourceGroup,
    
    [Parameter(Mandatory)]
    [string] $SubscriptionId,

    # Specify this on the first execution to get everything installed in powershell. It does not need to be run every time.
    [Parameter()]
    [switch] $InstallDependencies
)

$startTime = (Get-Date)

########################################################################################################
# Set error and warning preferences for the script to run.
########################################################################################################

$ErrorActionPreference = "Stop"
$WarningActionPreference = "Continue"

########################################################################################################
# Log the values of optional parameters passed
########################################################################################################

Write-Host "`nInstallDependencies $InstallDependencies"

###########################################################################
# Connect-AzureSubscription - gets current Azure context or triggers a 
# user log in to Azure. Selects the Azure subscription for creation of 
# the virtual machine.
###########################################################################

Function Connect-AzureSubscription()
{
    # Ensure the user is logged in
    try
    {
        $azureContext = az account show
    }
    catch
    {
    }

    if (-not $azureContext)
    {
        Write-Host "`nPlease login to Azure."
        az login
        $azureContext = az account show
    }

    # Ensure the desired subscription is selected
    $sub = az account show --output tsv --query id
    if ($sub -ne $SubscriptionId)
    {
        Write-Host "`nSelecting subscription $SubscriptionId"
        az account set --subscription $SubscriptionId
    }

    return $azureContext
}

Function Check-AzureCliVersion()
{
    $azCliVersionTested = [System.Version]"2.37.0"

    $azCliVersionCurrentString = az version --query '\"azure-cli\"'
    $azCliVersionCurrent = [System.Version]($azCliVersionCurrentString.Trim('"'))

    if ($azCliVersionTested -gt $azCliVersionCurrent)
    {
        Write-Host "`nVersion of Azure CLI installed is $azCliVersionCurrent while this script has been tested on a newer version of $azCliVersionTested."
        Write-Host "`nUpdating Azure CLI version to $azCliVersionTested."

        az upgrade
    }
}

Function Require-Admin()
{
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    if (-not $isAdmin)
    {
        throw "This script must be run in administrative mode."
    }
}

#################################################################################################
# Set required parameters.
#################################################################################################

$Region = $Region.Replace(' ', '')
$iothubUnitsToBeCreated = 1

#################################################################################################
# Get resource names to pass to deployment
#################################################################################################

## remove any characters that aren't letters or numbers, and then validate
$storageAccountName = "$($ResourceGroup.ToLower())sa"
$storageAccountName = [regex]::Replace($storageAccountName, "[^a-z0-9]", "")
if (-not ($storageAccountName -match "^[a-z0-9][a-z0-9]{1,22}[a-z0-9]$"))
{
    throw "Storage account name derived from resource group has illegal characters: $storageAccountName"
}

$keyVaultName = "env-$ResourceGroup-kv";
$keyVaultName = [regex]::Replace($keyVaultName, "[^a-zA-Z0-9-]", "")
if (-not ($keyVaultName -match "^[a-zA-Z][a-zA-Z0-9-]{1,22}[a-zA-Z0-9]$"))
{
    throw "Key vault name derived from resource group has illegal characters: $keyVaultName";
}

########################################################################################################
# Install latest version of az cli.
########################################################################################################

if ($InstallDependencies)
{
    Require-Admin
    Write-Host "`nInstalling and updating AZ CLI."
    Install-Module -Name Az -AllowClobber -Force
    Update-Module -Name Az
}

Check-AzureCliVersion

#######################################################################################################
# Install azure iot extension.
#######################################################################################################

if ($InstallDependencies)
{
    Require-Admin
    Write-Host "`nInstalling azure iot cli extensions."
    az extension add --name azure-iot
}

######################################################################################################
# Setup azure context.
######################################################################################################

$azureContext = Connect-AzureSubscription
$userObjectId = az ad signed-in-user show --query id --output tsv

######################################################################################################
# Get-ResourceGroup - Finds or creates the resource group to be used by the
# deployment.
######################################################################################################

$rgExists = az group exists --name $ResourceGroup
if ($rgExists -eq "False")
{
    Write-Host "`nCreating resource group $ResourceGroup in $Region"
    az group create --name $ResourceGroup --location $Region --output none
}

$resourceGroupId = az group show -n $ResourceGroup --query id --out tsv

#######################################################################################################
# Invoke-Deployment - Uses the .\.json template to create the necessary resources to run LongHaul tests.
#######################################################################################################

# Create a unique deployment name
$randomSuffix = -join ((65..90) + (97..122) | Get-Random -Count 5 | ForEach-Object { [char]$_ })
$deploymentName = "IotLongHaulInfra-$randomSuffix"

# Deploy
Write-Host @"
    `nStarting deployment which may take a while.
    1. Progress can be monitored from the Azure Portal (http://portal.azure.com); go to resource group | deployments | deployment name.
    2. Info to track: subscription ($SubscriptionId), resource group ($ResourceGroup), deployment name ($deploymentName).
"@

az deployment group create `
    --resource-group $ResourceGroup `
    --name $deploymentName `
    --output none `
    --only-show-errors `
    --template-file "$PSScriptRoot\longhaul-resources.json" `
    --parameters `
    UserObjectId=$userObjectId `
    StorageAccountName=$storageAccountName `
    KeyVaultName=$keyVaultName `
    HubUnitsCount=$iothubUnitsToBeCreated `

if ($LastExitCode -ne 0)
{
    throw "Error running resource group deployment."
}

Write-Host "`nYour infrastructure is ready in subscription ($SubscriptionId), resource group ($ResourceGroup)."

#########################################################################################################
# Get propreties to setup the config file for Environment variables.
#########################################################################################################

Write-Host "`nGetting generated names and secrets from ARM template output."
$iotHubConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.hubConnectionString.value' --output tsv
$storageAccountConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName  --query 'properties.outputs.storageAccountConnectionString.value' --output tsv
$keyVaultName = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.keyVaultName.value' --output tsv
$iotHubName = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.hubName.value' --output tsv
$instrumentationKey = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.instrumentationKey.value' --output tsv

##################################################################################################################################
# Fetch the iothubowner policy details.
##################################################################################################################################

$iothubownerSasPolicy = "iothubowner"
$iothubownerSasPrimaryKey = az iot hub policy show --hub-name $iotHubName --name $iothubownerSasPolicy --query 'primaryKey'

##################################################################################################################################
# Create device in IoT hub.
##################################################################################################################################

$longhaulDeviceId = "LongHaulDevice1"
$longhaulDevice = az iot hub device-identity list -g $ResourceGroup --hub-name $iotHubName --query "[?deviceId=='$longhaulDeviceId'].deviceId" --output tsv

if (-not $longhaulDevice)
{
    Write-Host "`nCreating X509 CA certificate authenticated device $longhaulDeviceId on IoT hub."
    az iot hub device-identity create -g $ResourceGroup --hub-name $iotHubName --device-id $longhaulDeviceId --am shared_private_key
}

$longhaulDeviceConnectionString = az iot hub device-identity connection-string show --device-id $longhaulDeviceId --hub-name $iotHubName --resource-group $ResourceGroup --output tsv

############################################################################################################################
# Store all secrets in a KeyVault - Values will be pulled down from here to configure environment variables.
############################################################################################################################

$keyvaultKvps = @{
    # Environment variables for IoT Hub E2E tests
    "IOTHUB-LONG-HAUL-DEVICE-CONNECTION-STRING" = $longhaulDeviceConnectionString;
    "APPLICATION-INSIGHTS-INSTRUMENTATION-KEY" = $instrumentationKey;
    "STORAGE-ACCOUNT-CONNECTION-STRING" = $storageAccountConnectionString;
}

Write-Host "`nWriting secrets to KeyVault $keyVaultName."
az keyvault set-policy -g $ResourceGroup --name $keyVaultName --object-id "$userObjectId" --output none --only-show-errors --secret-permissions delete get list set;
foreach ($kvp in $keyvaultKvps.GetEnumerator())
{
    Write-Host "`tWriting $($kvp.Name)."
    if ($null -eq $kvp.Value)
    {
        Write-Warning "`t`tValue is unexpectedly null!";
    }
    az keyvault secret set --vault-name $keyVaultName --name $kvp.Name --value "$($kvp.Value)" --output none --only-show-errors
}

############################################################################################################################
# Creating a file to run to load environment variables
############################################################################################################################

$loadScriptDir = Join-Path $PSScriptRoot "..\..\..\.." -Resolve
$loadScriptName = "Load-$keyVaultName.ps1";
Write-Host "`nWriting environment loading file to $loadScriptDir\$loadScriptName.`n"
$file = New-Item -Path $loadScriptDir -Name $loadScriptName -ItemType "file" -Force
Add-Content -Path $file.PSPath -Value "$loadScriptDir\azure-iot-sdk-csharp\e2e\Tests\prerequisites\LoadEnvironmentVariablesFromKeyVault.ps1 -SubscriptionId $SubscriptionId -KeyVaultName $keyVaultName"

############################################################################################################################
# Configure environment variables
############################################################################################################################

Invoke-Expression "$loadScriptDir\$loadScriptName"

$endTime = (Get-Date)
$elapsedTime = (($endTime - $startTime).TotalMinutes).ToString("N1")
Write-Host "`n`nCompleted in $elapsedTime minutes.`n`t- For future sessions, run the generated file $loadScriptDir\$loadScriptName to load environment variables.`n`t- Values will be overwritten if you run LongHaulSetup.ps1 with a same resource group name.`n"
