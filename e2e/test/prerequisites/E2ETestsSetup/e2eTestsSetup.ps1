# NOTE: This script needs to be run using admin mode

param(
    [Parameter(Mandatory)]
    [string] $Region,

    [Parameter(Mandatory)]
    [string] $ResourceGroup,
    
    [Parameter(Mandatory)]
    [string] $SubscriptionId,

    [Parameter(Mandatory)]
    [string] $GroupCertificatePassword,

    # Specify this on the first execution to get everything installed in powershell. It does not need to be run every time.
    [Parameter()]
    [bool] $InstallDependencies,

    # Set this to true if you are generating resources for the DevOps test pipeline.
    # This will create resources capable of handling the test pipeline traffic, which is greater than what you would generally require for local testing.
    [Parameter()]
    [bool] $GenerateResourcesForDevOpsPipeline,

    # Set this to true if you would like to enable security solutions for your IoT Hub.
    # Security solution for IoT Hub enables you to route security messages to a specific Log Analytics Workspace.
    [Parameter()]
    [bool] $EnableIotHubSecuritySolution
)

$startTime = (Get-Date)

########################################################################################################
# Set error and warning preferences for the script to run
########################################################################################################

$ErrorActionPreference = "Stop"
$WarningActionPreference = "Continue"

########################################################################################################
# Log the values of optional parameters passed
########################################################################################################

Write-Host "`nInstallDependencies $InstallDependencies"
Write-Host "`nGenerateResourcesForDevOpsPipeline $GenerateResourcesForDevOpsPipeline"
Write-Host "`nEnableIotHubSecuritySolution $EnableIotHubSecuritySolution"

###########################################################################
# Connect-AzureSubscription - gets current Azure context or triggers a 
# user log in to Azure. Selects the Azure subscription for creation of 
# the virtual machine
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

Function CleanUp-Certs()
{
    Write-Host "`nCleaning up old certs and files that may cause conflicts."
    $certsToDelete1 = Get-ChildItem "Cert:\LocalMachine\My" | Where-Object { $_.Issuer.Contains("CN=$subjectPrefix") }
    $certsToDelete2 = Get-ChildItem "Cert:\LocalMachine\My" | Where-Object { $_.Issuer.Contains("CN=$groupCertCommonName") }
    $certsToDelete3 = Get-ChildItem "Cert:\LocalMachine\My" | Where-Object { $_.Issuer.Contains("CN=$deviceCertCommonName") }

    $certsToDelete = $certsToDelete1 + $certsToDelete2 + $certsToDelete3
    
    $title = "Cleaning up certs."
    $certsToDeleteSubjectNames = $certsToDelete | foreach-object  {$_.Subject}
    $certsToDeleteSubjectNames = $certsToDeleteSubjectNames -join "`n"
    $question = "Are you sure you want to delete the following certs?`n`n$certsToDeleteSubjectNames"
    $choices  = '&Yes', '&No'
    $decision = $Host.UI.PromptForChoice($title, $question, $choices, 1)

    if ($certsToDelete.Count -ne 0)
    {
        if($decision -eq 0)
        {
            #Remove
            Write-Host '`tConfirmed.'
            $certsToDelete | Remove-Item
        }
        else
        {
            #Don't remove certs and exit
            Write-Host '`tCancelled.'
            exit
        }
    }

    Get-ChildItem $PSScriptRoot | Where-Object { $_.Name.EndsWith(".pfx") } | Remove-Item
    Get-ChildItem $PSScriptRoot | Where-Object { $_.Name.EndsWith(".cer") } | Remove-Item
    Get-ChildItem $PSScriptRoot | Where-Object { $_.Name.EndsWith(".p7b") } | Remove-Item
}

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin)
{
    throw "This script must be run in administrative mode."
}

#################################################################################################
# Set required parameters
#################################################################################################

$Region = $Region.Replace(' ', '')
$logAnalyticsAppRegnName = "$ResourceGroup-LogAnalyticsAadApp"
$iotHubAadTestAppRegName = "$ResourceGroup-IotHubAadApp"
$uploadCertificateName = "group1-certificate"
$hubUploadCertificateName = "rootCA"
$iothubUnitsToBeCreated = 1
$managedIdentityName = "$ResourceGroup-user-msi"

# OpenSSL has dropped support for SHA1 signed certificates in Ubuntu 20.04, so our test resources will use SHA256 signed certificates instead.
$certificateHashAlgorithm = "SHA256"

#################################################################################################
# Make any special modifications required to generate resources for the DevOps test pipeline
#################################################################################################

if ($GenerateResourcesForDevOpsPipeline)
{
    $iothubUnitsToBeCreated = 3;
}


#################################################################################################
# Get Function App contents to pass to deployment
#################################################################################################

$dpsCustomAllocatorRunCsxPath = Resolve-Path $PSScriptRoot/DpsCustomAllocatorFunctionFiles/run.csx
$dpsCustomAllocatorProjPath = Resolve-Path $PSScriptRoot/DpsCustomAllocatorFunctionFiles/function.proj

# Read bytes from files
$dpsCustomAllocatorRunCsxBytes = [System.IO.File]::ReadAllBytes($dpsCustomAllocatorRunCsxPath);
$dpsCustomAllocatorProjBytes = [System.IO.File]::ReadAllBytes($dpsCustomAllocatorProjPath);

# convert contents to base64 string, which will be decoded in the ARM template to ensure all the characters are interpreted correctly
$dpsCustomAllocatorRunCsxContent = [System.Convert]::ToBase64String($dpsCustomAllocatorRunCsxBytes);
$dpsCustomAllocatorProjContent = [System.Convert]::ToBase64String($dpsCustomAllocatorProjBytes);

## remove any characters that aren't letters or numbers, and then validate
$storageAccountName = "$($ResourceGroup.ToLower())sa"
$storageAccountName = [regex]::Replace($storageAccountName, "[^a-z0-9]", "")
if (-not ($storageAccountName -match "^[a-z0-9][a-z0-9]{1,22}[a-z0-9]$"))
{
    throw "Storage account name derrived from resource group has illegal characters: $storageAccountName"
}

$keyVaultName = "env-$ResourceGroup-kv";
$keyVaultName = [regex]::Replace($keyVaultName, "[^a-zA-Z0-9-]", "")
if (-not ($keyVaultName -match "^[a-zA-Z][a-zA-Z0-9-]{1,22}[a-zA-Z0-9]$"))
{
    throw "Key vault name derrived from resource group has illegal characters: $keyVaultName";
}

########################################################################################################
# Generate self-signed certs and to use in DPS and IoT hub
# New certs will be generated each time you run the script as the script cleans up in the end
########################################################################################################

$subjectPrefix = "IoT Test";
$rootCommonName = "$subjectPrefix Test Root CA";
$intermediateCert1CommonName = "$subjectPrefix Intermediate 1 CA";
$intermediateCert2CommonName = "$subjectPrefix Intermediate 2 CA";
$groupCertCommonName = "xdevice1";
$deviceCertCommonName = "iothubx509device1";
$iotHubCertCommonName = "iothubx509device1";
$iotHubCertChainDeviceCommonName = "iothubx509chaindevice1";

$rootCertPath = "$PSScriptRoot/Root.cer";
$individualDeviceCertPath = "$PSScriptRoot/Device.cer";
$verificationCertPath = "$PSScriptRoot/verification.cer";

$groupPfxPath = "$PSScriptRoot/Group.pfx";
$individualDevicePfxPath = "$PSScriptRoot/Device.pfx";
$iotHubPfxPath = "$PSScriptRoot/IotHub.pfx";
$iotHubChainDevicPfxPath = "$PSScriptRoot/IotHubChainDevice.pfx";
$intermediateCert1CertPath = "$PSScriptRoot/intermediateCert1.cer";
$intermediateCert2CertPath = "$PSScriptRoot/intermediateCert2.cer";

$groupCertChainPath = "$PSScriptRoot/GroupCertChain.p7b";

############################################################################################################################
# Cleanup old certs and files that can cause a conflict
############################################################################################################################
CleanUp-Certs

# Generate self signed Root and Intermediate CA cert, expiring in 2 years
# These certs are used for signing so ensure to have the correct KeyUsage - CertSign and TestExtension - ca=TRUE&pathlength=12

Write-Host "`nGenerating self signed certs."

$rootCACert = New-SelfSignedCertificate `
    -DnsName "$rootCommonName" `
    -KeyUsage CertSign `
    -TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -HashAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)

$intermediateCert1 = New-SelfSignedCertificate `
    -DnsName "$intermediateCert1CommonName" `
    -KeyUsage CertSign `
    -TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -HashAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $rootCACert

$intermediateCert2 = New-SelfSignedCertificate `
    -DnsName "$intermediateCert2CommonName" `
    -KeyUsage CertSign `
    -TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -HashAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $intermediateCert1

# Create Certificate chain from Root to IntermediateCert2. This chain will be combined with the cert signed by IntermediateCert2 to test group enrollment.
# Chain: Root->Intermediate1->Intermediate2, cert: Intermediate2->deviceCert
Get-ChildItem "Cert:\LocalMachine\My" | Where-Object { $_.Issuer.contains("CN=$subjectPrefix") } | Export-Certificate -FilePath $groupCertChainPath -Type p7b | Out-Null

Export-Certificate -cert $rootCACert -FilePath $rootCertPath -Type CERT | Out-Null
$iothubX509RootCACertificate = [Convert]::ToBase64String((Get-Content $rootCertPath -AsByteStream))

$certPassword = ConvertTo-SecureString $GroupCertificatePassword -AsPlainText -Force

# Create leaf certificates, expiring in 2 years
# These certs are not used for signing so don't specify KeyUsage and TestExtension - ca=TRUE&pathlength=12

# Certificate for enrollment of a device using group enrollment.
$groupDeviceCert = New-SelfSignedCertificate `
    -DnsName "$groupCertCommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -HashAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $intermediateCert2

Export-PFXCertificate -cert $groupDeviceCert -filePath $groupPfxPath -password $certPassword | Out-Null
$dpsGroupX509PfxCertificate = [Convert]::ToBase64String((Get-Content $groupPfxPath -AsByteStream));

# Certificate for enrollment of a device using individual enrollment.
$individualDeviceCert = New-SelfSignedCertificate `
    -DnsName "$deviceCertCommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -HashAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)

Export-Certificate -cert $individualDeviceCert -FilePath $individualDeviceCertPath -Type CERT | Out-Null
Export-PFXCertificate -cert $individualDeviceCert -filePath $individualDevicePfxPath -password $certPassword | Out-Null
$dpsIndividualX509PfxCertificate = [Convert]::ToBase64String((Get-Content $individualDevicePfxPath -AsByteStream));

# IoT hub certificate for authentication. The tests are not setup to use a password for the certificate so create the certificate is created with no password.
$iotHubCert = New-SelfSignedCertificate `
    -DnsName "$iotHubCertCommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -HashAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)

# IoT hub certificate signed by intermediate certificate for authentication.
$iotHubChainDeviceCert = New-SelfSignedCertificate `
    -DnsName "$iotHubCertChainDeviceCommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -HashAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $intermediateCert2

$iotHubCredentials = New-Object System.Management.Automation.PSCredential("Password", (New-Object System.Security.SecureString))
Export-PFXCertificate -cert $iotHubCert -filePath $iotHubPfxPath -password $iotHubCredentials.Password | Out-Null
$iothubX509PfxCertificate = [Convert]::ToBase64String((Get-Content $iotHubPfxPath -AsByteStream));

$iotHubCredentials = New-Object System.Management.Automation.PSCredential("Password", (New-Object System.Security.SecureString))
Export-PFXCertificate -cert $iotHubChainDeviceCert -filePath $iotHubChainDevicPfxPath -password $iotHubCredentials.Password | Out-Null
$iothubX509ChainDevicePfxCertificate = [Convert]::ToBase64String((Get-Content $iotHubChainDevicPfxPath -AsByteStream));

Export-Certificate -cert $intermediateCert1 -FilePath $intermediateCert1CertPath -Type CERT | Out-Null
$iothubX509Intermediate1Certificate = [Convert]::ToBase64String((Get-Content $intermediateCert1CertPath -AsByteStream));

Export-Certificate -cert $intermediateCert2 -FilePath $intermediateCert2CertPath -Type CERT | Out-Null
$iothubX509Intermediate2Certificate = [Convert]::ToBase64String((Get-Content $intermediateCert2CertPath -AsByteStream));

$dpsGroupX509CertificateChain = [Convert]::ToBase64String((Get-Content $groupCertChainPath -AsByteStream));
$dpsX509PfxCertificatePassword = $GroupCertificatePassword;

########################################################################################################
# Install latest version of az cli
########################################################################################################

if ($InstallDependencies)
{
    Write-Host "`nInstalling and updating AZ CLI."
    Install-Module -Name Az -AllowClobber -Force
    Update-Module -Name Az
}

########################################################################################################
# Install chocolatey and docker
########################################################################################################

if ($InstallDependencies)
{
    Write-Host "`nSetting up docker."
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'));
    choco install docker-desktop -y
    # Refresh paths after installation of choco
    refreshenv
    docker pull aziotbld/testtpm
    docker pull aziotbld/testproxy
}

#######################################################################################################
# Install azure iot extension
#######################################################################################################

if ($InstallDependencies)
{
    Write-Host "`nInstalling azure iot cli extensions."
    az extension add --name azure-iot
}

######################################################################################################
# Setup azure context
######################################################################################################

$azureContext = Connect-AzureSubscription
$userObjectId = az ad signed-in-user show --query objectId --output tsv

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
# Invoke-Deployment - Uses the .\.json template to
# create the necessary resources to run E2E tests.
#######################################################################################################

# Create a unique deployment name
$randomSuffix = -join ((65..90) + (97..122) | Get-Random -Count 5 | ForEach-Object { [char]$_ })
$deploymentName = "IotE2eInfra-$randomSuffix"

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
    --template-file "$PSScriptRoot\test-resources.json" `
    --parameters `
    UserObjectId=$userObjectId `
    StorageAccountName=$storageAccountName `
    KeyVaultName=$keyVaultName `
    DpsCustomAllocatorRunCsxContent=$dpsCustomAllocatorRunCsxContent `
    DpsCustomAllocatorProjContent=$dpsCustomAllocatorProjContent `
    HubUnitsCount=$iothubUnitsToBeCreated `
    UserAssignedManagedIdentityName=$managedIdentityName `
    EnableIotHubSecuritySolution=$EnableIotHubSecuritySolution

if ($LastExitCode -ne 0)
{
    throw "Error running resource group deployment."
}

Write-Host "`nYour infrastructure is ready in subscription ($SubscriptionId), resource group ($ResourceGroup)."

#########################################################################################################
# Get propreties to setup the config file for Environment variables
#########################################################################################################

$iotHubThumbprint = "CADB8E398FA9C7DD382E2ED092258BB3D916652C"
$proxyServerAddress = "127.0.0.1:8888"

Write-Host "`nGetting generated names and secrets from ARM template output."
$iotHubConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.hubConnectionString.value' --output tsv
$farHubHostName = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.farHubHostName.value' --output tsv
$farHubConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.farHubConnectionString.value' --output tsv
$dpsName = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.dpsName.value' --output tsv
$dpsConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName  --query 'properties.outputs.dpsConnectionString.value' --output tsv
$storageAccountConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName  --query 'properties.outputs.storageAccountConnectionString.value' --output tsv
$workspaceId = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.workspaceId.value' --output tsv
$customAllocationPolicyWebhook = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.customAllocationPolicyWebhook.value' --output tsv
$keyVaultName = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.keyVaultName.value' --output tsv
$instrumentationKey = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.instrumentationKey.value' --output tsv
$iotHubName = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.hubName.value' --output tsv

#################################################################################################################################################
# Configure an AAD app and create self signed certs and get the bytes to generate more content info.
#################################################################################################################################################
Write-Host "`nCreating app registration $logAnalyticsAppRegnName"
$logAnalyticsAppRegUrl = "http://$logAnalyticsAppRegnName"
$logAnalyticsAppId = az ad sp create-for-rbac -n $logAnalyticsAppRegUrl --role "Reader" --scope $resourceGroupId --query "appId" --output tsv
Write-Host "`nCreated application $logAnalyticsAppRegnName with Id $logAnalyticsAppId."

#################################################################################################################################################
# Configure an AAD app to perform IoT hub data actions.
#################################################################################################################################################
Write-Host "`nCreating app registration $iotHubAadTestAppRegName for IoT hub data actions"
$iotHubAadTestAppRegUrl = "http://$iotHubAadTestAppRegName"
$iotHubDataContributorRoleId = "4fc6c259987e4a07842ec321cc9d413f"
$iotHubScope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Devices/IotHubs/$iotHubName"
$iotHubAadTestAppInfo = az ad sp create-for-rbac -n $iotHubAadTestAppRegUrl --role $iotHubDataContributorRoleId --scope $iotHubScope --query '{appId:appId, password:password}' | ConvertFrom-Json
$iotHubAadTestAppPassword = $iotHubAadTestAppInfo.password
$iotHubAadTestAppId = $iotHubAadTestAppInfo.appId
Write-Host "`nCreated application $iotHubAadTestAppRegName with Id $iotHubAadTestAppId."

#################################################################################################################################################
# Add role assignement for User assinged managed identity to be able to perform import and export jobs on the IoT hub.
#################################################################################################################################################
Write-Host "`nGranting the user assigned managed identity $managedIdentityName Storage Blob Data Contributor permissions on resource group: $ResourceGroup."
$msiPrincipalId = az identity show -n $managedIdentityName -g $ResourceGroup --query principalId --output tsv
$msiResourceId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.ManagedIdentity/userAssignedIdentities/$managedIdentityName"
az role assignment create --assignee $msiPrincipalId --role 'Storage Blob Data Contributor' --scope $resourceGroupId --output none

##################################################################################################################################
# Granting the IoT hub system identity storage blob contributor access on the resoruce group
##################################################################################################################################
Write-Host "`nGranting the system identity on the hub $iotHubName Storage Blob Data Contributor permissions on resource group: $ResourceGroup."
$systemIdentityPrincipal = az resource list -n $iotHubName --query [0].identity.principalId --out tsv
az role assignment create --assignee $systemIdentityPrincipal --role "Storage Blob Data Contributor" --scope $resourceGroupId --output none

##################################################################################################################################
# Uploading root CA certificate to IoT hub and verifying
##################################################################################################################################

$certExits = az iot hub certificate list -g $ResourceGroup --hub-name $iotHubName --query "value[?name=='$hubUploadCertificateName']" --output tsv
if ($certExits)
{
    Write-Host "`nDeleting existing certificate from IoT hub."
    $etag = az iot hub certificate show -g $ResourceGroup --hub-name $iotHubName --name $hubUploadCertificateName --query 'etag'
    az iot hub certificate delete -g $ResourceGroup --hub-name $iotHubName --name $hubUploadCertificateName --etag $etag
}
Write-Host "`nUploading new certificate to IoT hub."
az iot hub certificate create -g $ResourceGroup --path $rootCertPath --hub-name $iotHubName --name $hubUploadCertificateName --output none

$isVerified = az iot hub certificate show -g $ResourceGroup --hub-name $iotHubName --name $hubUploadCertificateName --query 'properties.isVerified' --output tsv
if ($isVerified -eq 'false')
{
    Write-Host "`nVerifying certificate uploaded to IoT hub."
    $etag = az iot hub certificate show -g $ResourceGroup --hub-name $iotHubName --name $hubUploadCertificateName --query 'etag'
    $requestedCommonName = az iot hub certificate generate-verification-code -g $ResourceGroup --hub-name $iotHubName --name $hubUploadCertificateName -e $etag --query 'properties.verificationCode'
    $verificationCertArgs = @{
        "-DnsName"                       = $requestedCommonName;
        "-CertStoreLocation"             = "cert:\LocalMachine\My";
        "-NotAfter"                      = (get-date).AddYears(2);
        "-TextExtension"                 = @("2.5.29.37={text}1.3.6.1.5.5.7.3.2,1.3.6.1.5.5.7.3.1", "2.5.29.19={text}ca=FALSE&pathlength=0");
        "-HashAlgorithm"                 = $certificateHashAlgorithm;
        "-Signer"                        = $rootCACert;
    }
    $verificationCert = New-SelfSignedCertificate @verificationCertArgs
    Export-Certificate -cert $verificationCert -filePath $verificationCertPath -Type Cert | Out-Null
    $etag = az iot hub certificate show -g $ResourceGroup --hub-name $iotHubName --name $hubUploadCertificateName --query 'etag'
    az iot hub certificate verify -g $ResourceGroup --hub-name $iotHubName --name $hubUploadCertificateName -e $etag --path $verificationCertPath --output none
}

##################################################################################################################################
# Fetch the iothubowner policy details
##################################################################################################################################
$iothubownerSasPolicy = "iothubowner"
$iothubownerSasPrimaryKey = az iot hub policy show --hub-name $iotHubName --name $iothubownerSasPolicy --query 'primaryKey'

##################################################################################################################################
# Create device in IoT hub that uses a certificate signed by intermediate certificate
##################################################################################################################################

$iotHubCertChainDevice = az iot hub device-identity list -g $ResourceGroup --hub-name $iotHubName --query "[?deviceId=='$iotHubCertChainDeviceCommonName'].deviceId" --output tsv

if (-not $iotHubCertChainDevice)
{
    Write-Host "`nCreating X509 CA certificate authenticated device $iotHubCertChainDeviceCommonName on IoT hub."
    az iot hub device-identity create -g $ResourceGroup --hub-name $iotHubName --device-id $iotHubCertChainDeviceCommonName --am x509_ca
}

##################################################################################################################################
# Create the IoT devices and modules that are used by the .NET samples
##################################################################################################################################
$iotHubSasBasedDeviceId = "DoNotDeleteDevice1"
$iotHubSasBasedDevice = az iot hub device-identity list -g $ResourceGroup --hub-name $iotHubName --query "[?deviceId=='$iotHubSasBasedDeviceId'].deviceId" --output tsv

if (-not $iotHubSasBasedDevice)
{
    Write-Host "`nCreating SAS-based device $iotHubSasBasedDeviceId on IoT hub."
    az iot hub device-identity create -g $ResourceGroup --hub-name $iotHubName --device-id $iotHubSasBasedDeviceId --ee
}
$iotHubSasBasedDeviceConnectionString = az iot hub device-identity connection-string show --device-id $iotHubSasBasedDeviceId --hub-name $iotHubName --resource-group $ResourceGroup --output tsv

$iotHubSasBasedModuleId = "DoNotDeleteModule1"
$iotHubSasBasedModule = az iot hub module-identity list -g $ResourceGroup --hub-name $iotHubName --device-id $iotHubSasBasedDeviceId --query "[?moduleId=='$iotHubSasBasedModuleId'].moduleId" --output tsv

if (-not $iotHubSasBasedModule)
{
    Write-Host "`nCreating SAS based module $iotHubSasBasedModuleId under device $iotHubSasBasedDeviceId on IoT hub."
    az iot hub module-identity create -g $ResourceGroup --hub-name $iotHubName --device-id $iotHubSasBasedDeviceId --module-id $iotHubSasBasedModuleId
}
$iotHubSasBasedModuleConnectionString = az iot hub module-identity connection-string show --device-id $iotHubSasBasedDeviceId --module-id $iotHubSasBasedModuleId --hub-name $iotHubName --resource-group $ResourceGroup --output tsv

$thermostatSampleDeviceId = "ThermostatSample_DoNotDelete"
$thermostatSampleDevice = az iot hub device-identity list -g $ResourceGroup --hub-name $iotHubName --query "[?deviceId=='$thermostatSampleDeviceId'].deviceId" --output tsv

if (-not $thermostatSampleDevice)
{
    Write-Host "`nCreating SAS-based device $thermostatSampleDeviceId on IoT hub."
    az iot hub device-identity create -g $ResourceGroup --hub-name $iotHubName --device-id $thermostatSampleDeviceId --ee
}
$thermostatSampleDeviceConnectionString = az iot hub device-identity connection-string show --device-id $thermostatSampleDeviceId --hub-name $iotHubName --resource-group $ResourceGroup --output tsv

$temperatureControllerSampleDeviceId = "TemperatureControllerSample_DoNotDelete"
$temperatureControllerSampleDevice = az iot hub device-identity list -g $ResourceGroup --hub-name $iotHubName --query "[?deviceId=='$temperatureControllerSampleDeviceId'].deviceId" --output tsv

if (-not $temperatureControllerSampleDevice)
{
    Write-Host "`nCreating SAS-based device $temperatureControllerSampleDeviceId on IoT hub."
    az iot hub device-identity create -g $ResourceGroup --hub-name $iotHubName --device-id $temperatureControllerSampleDeviceId --ee
}
$temperatureControllerSampleDeviceConnectionString = az iot hub device-identity connection-string show --device-id $temperatureControllerSampleDeviceId --hub-name $iotHubName --resource-group $ResourceGroup --output tsv

##################################################################################################################################
# Create the DPS enrollments that are used by the .NET samples
##################################################################################################################################

$symmetricKeySampleEnrollmentRegistrationId = "SymmetricKeySampleIndividualEnrollment"
$symmetricKeyEnrollmentExists = az iot dps enrollment list -g $ResourceGroup  --dps-name $dpsName --query "[?deviceId=='$symmetricKeySampleEnrollmentRegistrationId'].deviceId" --output tsv
if ($symmetricKeyEnrollmentExists)
{
    Write-Host "`nDeleting existing individual enrollment $symmetricKeySampleEnrollmentRegistrationId."
    az iot dps enrollment delete -g $ResourceGroup --dps-name $dpsName --enrollment-id $symmetricKeySampleEnrollmentRegistrationId
}
Write-Host "`nAdding individual enrollment $symmetricKeySampleEnrollmentRegistrationId."
az iot dps enrollment create -g $ResourceGroup --dps-name $dpsName --enrollment-id $symmetricKeySampleEnrollmentRegistrationId --attestation-type symmetrickey --output none

$symmetricKeySampleEnrollmentPrimaryKey = az iot dps enrollment show -g $ResourceGroup --dps-name $dpsName --enrollment-id $symmetricKeySampleEnrollmentRegistrationId --show-keys --query 'attestation.symmetricKey.primaryKey' --output tsv

##################################################################################################################################
# Uploading certificate to DPS, verifying, and creating enrollment groups
##################################################################################################################################

$dpsIdScope = az iot dps show -g $ResourceGroup --name $dpsName --query 'properties.idScope' --output tsv
$certExits = az iot dps certificate list -g $ResourceGroup --dps-name $dpsName --query "value[?name=='$uploadCertificateName']" --output tsv
if ($certExits)
{
    Write-Host "`nDeleting existing certificate from DPS."
    $etag = az iot dps certificate show -g $ResourceGroup --dps-name $dpsName --certificate-name $uploadCertificateName --query 'etag'
    az iot dps certificate delete -g $ResourceGroup --dps-name $dpsName --name $uploadCertificateName --etag $etag
}
Write-Host "`nUploading new certificate to DPS."
az iot dps certificate create -g $ResourceGroup --path $rootCertPath --dps-name $dpsName --certificate-name $uploadCertificateName --output none

$isVerified = az iot dps certificate show -g $ResourceGroup --dps-name $dpsName --certificate-name $uploadCertificateName --query 'properties.isVerified' --output tsv
if ($isVerified -eq 'false')
{
    Write-Host "`nVerifying certificate uploaded to DPS."
    $etag = az iot dps certificate show -g $ResourceGroup --dps-name $dpsName --certificate-name $uploadCertificateName --query 'etag'
    $requestedCommonName = az iot dps certificate generate-verification-code -g $ResourceGroup --dps-name $dpsName --certificate-name $uploadCertificateName -e $etag --query 'properties.verificationCode'
    $verificationCertArgs = @{
        "-DnsName"             = $requestedCommonName;
        "-CertStoreLocation"   = "cert:\LocalMachine\My";
        "-NotAfter"            = (get-date).AddYears(2);
        "-TextExtension"       = @("2.5.29.37={text}1.3.6.1.5.5.7.3.2,1.3.6.1.5.5.7.3.1", "2.5.29.19={text}ca=FALSE&pathlength=0");
        "-HashAlgorithm"       = $certificateHashAlgorithm;
        "-Signer"              = $rootCACert;
    }
    $verificationCert = New-SelfSignedCertificate @verificationCertArgs
    Export-Certificate -cert $verificationCert -filePath $verificationCertPath -Type Cert | Out-Null
    $etag = az iot dps certificate show -g $ResourceGroup --dps-name $dpsName --certificate-name $uploadCertificateName --query 'etag'
    az iot dps certificate verify -g $ResourceGroup --dps-name $dpsName --certificate-name $uploadCertificateName -e $etag --path $verificationCertPath --output none
}

$groupEnrollmentId = "Group1"
$groupEnrollmentExists = az iot dps enrollment-group list -g $ResourceGroup --dps-name $dpsName --query "[?enrollmentGroupId=='$groupEnrollmentId'].enrollmentGroupId" --output tsv
if ($groupEnrollmentExists)
{
    Write-Host "`nDeleting existing group enrollment $groupEnrollmentId."
    az iot dps enrollment-group delete -g $ResourceGroup --dps-name $dpsName --enrollment-id $groupEnrollmentId
}
Write-Host "`nAdding group enrollment $groupEnrollmentId."
az iot dps enrollment-group create -g $ResourceGroup --dps-name $dpsName --enrollment-id $groupEnrollmentId --ca-name $uploadCertificateName --output none

$individualEnrollmentId = "iothubx509device1"
$individualDeviceId = "provisionedx509device1"
$individualEnrollmentExists = az iot dps enrollment list -g $ResourceGroup  --dps-name $dpsName --query "[?deviceId=='$individualDeviceId'].deviceId" --output tsv
if ($individualEnrollmentExists)
{
    Write-Host "`nDeleting existing individual enrollment $individualEnrollmentId for device $individualDeviceId."
    az iot dps enrollment delete -g $ResourceGroup --dps-name $dpsName --enrollment-id $individualEnrollmentId
}
Write-Host "`nAdding individual enrollment $individualEnrollmentId for device $individualDeviceId."
az iot dps enrollment create `
    -g $ResourceGroup `
    --dps-name $dpsName `
    --enrollment-id $individualEnrollmentId `
    --device-id $individualDeviceId `
    --attestation-type x509 `
    --certificate-path $individualDeviceCertPath `
    --output none

Write-Host "`nCreating a self-signed certificate and placing it in $ResourceGroup."
az ad app credential reset --id $logAnalyticsAppId --create-cert --keyvault $keyVaultName --cert $ResourceGroup --output none
Write-Host "`nSuccessfully created a self signed certificate for your application $logAnalyticsAppRegnName in $keyVaultName key vault with cert name $ResourceGroup."

Write-Host "`nFetching the certificate binary."
$selfSignedCerts = "$PSScriptRoot\selfSignedCerts"
if (Test-Path $selfSignedCerts -PathType Leaf)
{
    Remove-Item -r $selfSignedCerts
}

az keyvault secret download --file $selfSignedCerts --vault-name $keyVaultName -n $ResourceGroup --encoding base64
$fileContent = Get-Content $selfSignedCerts -AsByteStream
$fileContentB64String = [System.Convert]::ToBase64String($fileContent);

Write-Host "`nSuccessfully fetched the certificate bytes. Removing the cert file from the disk."
Remove-Item -r $selfSignedCerts

###################################################################################################################################
# Store all secrets in a KeyVault - Values will be pulled down from here to configure environment variables
###################################################################################################################################

Write-Host "`nWriting secrets to KeyVault $keyVaultName."
az keyvault set-policy -g $ResourceGroup --name $keyVaultName --object-id $userObjectId --secret-permissions delete get list set --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-CONNECTION-STRING" --value $iotHubConnectionString --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-PFX-X509-THUMBPRINT" --value $iotHubThumbprint --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-PROXY-SERVER-ADDRESS" --value $proxyServerAddress --output none
az keyvault secret set --vault-name $keyVaultName --name "FAR-AWAY-IOTHUB-HOSTNAME" --value $farHubHostName --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-IDSCOPE" --value $dpsIdScope --output none
az keyvault secret set --vault-name $keyVaultName --name "PROVISIONING-CONNECTION-STRING" --value $dpsConnectionString --output none
az keyvault secret set --vault-name $keyVaultName --name "CUSTOM-ALLOCATION-POLICY-WEBHOOK" --value $customAllocationPolicyWebhook --output none

$dpsEndpoint = "global.azure-devices-provisioning.net"
if ($Region.EndsWith('euap', 'CurrentCultureIgnoreCase'))
{
    $dpsEndpoint = "global-canary.azure-devices-provisioning.net"
}
az keyvault secret set --vault-name $keyVaultName --name "DPS-GLOBALDEVICEENDPOINT" --value $dpsEndpoint --output none

az keyvault secret set --vault-name $keyVaultName --name "DPS-X509-PFX-CERTIFICATE-PASSWORD" --value $dpsX509PfxCertificatePassword --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-X509-PFX-CERTIFICATE" --value $iothubX509PfxCertificate --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-INDIVIDUALX509-PFX-CERTIFICATE" --value $dpsIndividualX509PfxCertificate --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-GROUPX509-PFX-CERTIFICATE" --value $dpsGroupX509PfxCertificate --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-GROUPX509-CERTIFICATE-CHAIN" --value $dpsGroupX509CertificateChain --output none
az keyvault secret set --vault-name $keyVaultName --name "STORAGE-ACCOUNT-CONNECTION-STRING" --value $storageAccountConnectionString --output none
az keyvault secret set --vault-name $keyVaultName --name "LA-WORKSPACE-ID" --value $workspaceId --output none
az keyvault secret set --vault-name $keyVaultName --name "MSFT-TENANT-ID" --value "72f988bf-86f1-41af-91ab-2d7cd011db47" --output none
az keyvault secret set --vault-name $keyVaultName --name "LA-AAD-APP-ID" --value $logAnalyticsAppId --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-CLIENT-ID" --value $iotHubAadTestAppId --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-CLIENT-SECRET" --value $iotHubAadTestAppPassword --output none
az keyvault secret set --vault-name $keyVaultName --name "LA-AAD-APP-CERT-BASE64" --value $fileContentB64String --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-GLOBALDEVICEENDPOINT-INVALIDCERT" --value "invalidcertgde1.westus.cloudapp.azure.com" --output none
az keyvault secret set --vault-name $keyVaultName --name "PIPELINE-ENVIRONMENT" --value "prod" --output none
az keyvault secret set --vault-name $keyVaultName --name "HUB-CHAIN-DEVICE-PFX-CERTIFICATE" --value $iothubX509ChainDevicePfxCertificate --output none
az keyvault secret set --vault-name $keyVaultName --name "HUB-CHAIN-ROOT-CA-CERTIFICATE" --value $iothubX509RootCACertificate --output none
az keyvault secret set --vault-name $keyVaultName --name "HUB-CHAIN-INTERMEDIATE1-CERTIFICATE" --value $iothubX509Intermediate1Certificate --output none
az keyvault secret set --vault-name $keyVaultName --name "HUB-CHAIN-INTERMEDIATE2-CERTIFICATE" --value $iothubX509Intermediate2Certificate --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-X509-CHAIN-DEVICE-NAME" --value $iotHubCertChainDeviceCommonName --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-USER-ASSIGNED-MSI-RESOURCE-ID" --value $msiResourceId --output none

# These environment variables are only used in Java
az keyvault secret set --vault-name $keyVaultName --name "IOT-DPS-CONNECTION-STRING" --value $dpsConnectionString --output none # DPS Connection string Environment variable for Java
az keyvault secret set --vault-name $keyVaultName --name "IOT-DPS-ID-SCOPE" --value $dpsIdScope --output none # DPS ID Scope Environment variable for Java
az keyvault secret set --vault-name $keyVaultName --name "FAR-AWAY-IOTHUB-CONNECTION-STRING" --value $farHubConnectionString --output none
az keyvault secret set --vault-name $keyVaultName --name "IS-BASIC-TIER-HUB" --value "false" --output none

<#[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="fake shared access token")]#>
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-DEVICE-CONN-STRING-INVALIDCERT" --value "HostName=invalidcertiothub1.westus.cloudapp.azure.com;DeviceId=DoNotDelete1;SharedAccessKey=zWmeTGWmjcgDG1dpuSCVjc5ZY4TqVnKso5+g1wt/K3E=" --output none
<#[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="fake shared access token")]#>
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-CONN-STRING-INVALIDCERT" --value "HostName=invalidcertiothub1.westus.cloudapp.azure.com;SharedAccessKeyName=iothubowner;SharedAccessKey=Fk1H0asPeeAwlRkUMTybJasksTYTd13cgI7SsteB05U=" --output none
<#[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="fake shared access token")]#>
az keyvault secret set --vault-name $keyVaultName --name "PROVISIONING-CONNECTION-STRING-INVALIDCERT" --value "HostName=invalidcertdps1.westus.cloudapp.azure.com;SharedAccessKeyName=provisioningserviceowner;SharedAccessKey=lGO7OlXNhXlFyYV1rh9F/lUCQC1Owuh5f/1P0I1AFSY=" --output none

az keyvault secret set --vault-name $keyVaultName --name "E2E-IKEY" --value $instrumentationKey --output none

# These environment variables are used by .NET samples
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-DEVICE-CONN-STRING" --value $iotHubSasBasedDeviceConnectionString --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-MODULE-CONN-STRING" --value $iotHubSasBasedModuleConnectionString --output none
az keyvault secret set --vault-name $keyVaultName --name "PNP-TC-DEVICE-CONN-STRING" --value $temperatureControllerSampleDeviceConnectionString --output none
az keyvault secret set --vault-name $keyVaultName --name "PNP-THERMOSTAT-DEVICE-CONN-STRING" --value $thermostatSampleDeviceConnectionString --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-SAS-KEY" --value $iothubownerSasPrimaryKey --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-SAS-KEY-NAME" --value $iothubownerSasPolicy --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-SYMMETRIC-KEY-INDIVIDUAL-ENROLLMENT-REGISTRATION-ID" --value $symmetricKeySampleEnrollmentRegistrationId --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-SYMMETRIC-KEY-INDIVIDUAL-ENROLLEMNT-PRIMARY-KEY" --value $symmetricKeySampleEnrollmentPrimaryKey --output none

###################################################################################################################################
# Run docker containers for TPM simulators and proxy
###################################################################################################################################

if (-not (docker images -q aziotbld/testtpm))
{
    Write-Host "Setting up docker container for TPM simulator."
    docker run -d --restart unless-stopped --name azure-iot-tpmsim -p 127.0.0.1:2321:2321 -p 127.0.0.1:2322:2322 aziotbld/testtpm
}

if (-not (docker images -q aziotbld/testproxy))
{
    Write-Host "Setting up docker container for proxy."
    docker run -d --restart unless-stopped --name azure-iot-tinyproxy -p 127.0.0.1:8888:8888 aziotbld/testproxy
}

############################################################################################################################
# Clean up certs and files created by the script
############################################################################################################################
CleanUp-Certs

# Creating a file to run to load environment variables
$loadScriptDir = Join-Path $PSScriptRoot "..\..\..\..\.." -Resolve
$loadScriptName = "Load-$keyVaultName.ps1";
Write-Host "`nWriting environment loading file to $loadScriptDir\$loadScriptName.`n"
$file = New-Item -Path $loadScriptDir -Name $loadScriptName -ItemType "file" -Force
Add-Content -Path $file.PSPath -Value "$PSScriptRoot\LoadEnvironmentVariablesFromKeyVault.ps1 -SubscriptionId $SubscriptionId -KeyVaultName $keyVaultName"

############################################################################################################################
# Configure environment variables
############################################################################################################################
Invoke-Expression "$loadScriptDir\$loadScriptName"

$endTime = (Get-Date)
$elapsedTime = (($endTime - $startTime).TotalMinutes).ToString("N1")
Write-Host "`n`nCompleted in $elapsedTime minutes.`n`t- For future sessions, run the generated file $loadScriptDir\$loadScriptName to load environment variables.`n`t- Values will be overwritten if you run e2eTestsSetup.ps1 with a same resource group name.`n"