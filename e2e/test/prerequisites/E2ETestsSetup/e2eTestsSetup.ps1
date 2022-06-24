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
    
    # This needs to be updated to [Mandatory] once the DPS client certificate issuance feature is publicly available.
    [Parameter()]
    [string] $CertificateAuthorityApiKey,

    # This needs to be updated to [Mandatory] once the DPS client certificate issuance feature is publicly available.
    [Parameter()]
    [string] $CertificateAuthorityProfileId,

    # Specify this on the first execution to get everything installed in powershell. It does not need to be run every time.
    [Parameter()]
    [switch] $InstallDependencies,

    # Set this if you are generating resources for the E2E test DevOps pipeline.
    # This will create resources capable of handling the test pipeline traffic, which is greater than what you would generally require for local testing.
    [Parameter()]
    [switch] $GenerateResourcesForTestDevOpsPipeline,

    # Set this if you are generating resources for the samples DevOps pipeline.
    # This will generate the resources required for running the DevOps pipline for the .NET samples.
    [Parameter()]
    [switch] $GenerateResourcesForSamplesDevOpsPipeline,

    # Set this if you would like to enable security solutions for your IoT Hub.
    # Security solution for IoT Hub enables you to route security messages to a specific Log Analytics Workspace.
    [Parameter()]
    [switch] $EnableIotHubSecuritySolution
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
Write-Host "`GenerateResourcesForTestDevOpsPipeline $GenerateResourcesForTestDevOpsPipeline"
Write-Host "`GenerateResourcesForSamplesDevOpsPipeline $GenerateResourcesForSamplesDevOpsPipeline"
Write-Host "`EnableIotHubSecuritySolution $EnableIotHubSecuritySolution"

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

Function CleanUp-Certs()
{
    Write-Host "`nCleaning up old certs and files that may cause conflicts."
    $certsToDelete1 = Get-ChildItem "Cert:\LocalMachine\My" | Where-Object { $_.Issuer.Contains("CN=$subjectPrefix") }
    $certsToDelete2 = Get-ChildItem "Cert:\LocalMachine\My" | Where-Object { $_.Subject.Contains("CN=$dpsTrustBundleCertificateCommonName") }

    $certsToDelete = $certsToDelete1 + $certsToDelete2

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

Function Calculate-SasKey([string]$keyName, [string]$key, [string]$target, [int]$sasTokenValiditySeconds)
{
    # Add the assembly required to Url Encode data
    Add-Type -AssemblyName System.Web

    $expirationTime = [DateTimeOffset]::Now.AddSeconds($sasTokenValiditySeconds).ToUnixTimeSeconds()
    $audience = [System.Web.HttpUtility]::UrlEncode($target)
    $requestString = $audience + "`n" + $expirationTime

    $hmacsha256 = New-Object System.Security.Cryptography.HMACSHA256
    $hmacsha256.key = [Convert]::FromBase64String($key)

    $signature = $hmacsha256.ComputeHash([Text.Encoding]::UTF8.GetBytes($requestString))
    $signatureBase64 = [Convert]::ToBase64String($signature)

    $signatureBase64Encoded = [System.Web.HttpUtility]::UrlEncode($signatureBase64)
    $expirationTimeEncoded = [System.Web.HttpUtility]::UrlEncode($expirationTime)
    $keyNameEncoded = [System.Web.HttpUtility]::UrlEncode($keyName)

    return "SharedAccessSignature sr=$audience&sig=$signatureBase64Encoded&se=$expirationTimeEncoded&skn=$keyNameEncoded"
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
$e2eTestAadAppRegName = "$ResourceGroup-E2eTestAadApp"
$dpsUploadCertificateName = "group1-certificate"
$hubUploadCertificateName = "rootCA"
$iothubUnitsToBeCreated = 1
$managedIdentityName = "$ResourceGroup-user-msi"
$dpsCaName = "$ResourceGroup-CaName"
$dpsTrustBundleId = "$ResourceGroup-TrustBundleId"

# OpenSSL has dropped support for SHA1 signed certificates in Ubuntu 20.04, so our test resources will use SHA256 signed certificates instead.
$certificateHashAlgorithm = "SHA256"

# DPS service API version for preview features
$dpsServiceApiPreviewVersion = "2021-11-01-preview"

#################################################################################################
# Make any special modifications required to generate resources for the DevOps test pipeline
#################################################################################################

if ($GenerateResourcesForTestDevOpsPipeline)
{
    $iothubUnitsToBeCreated = 5;
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
if (-not ($keyVaultName -match "^[a-zA-Z][a-zA-Z0-9-]{1,24}[a-zA-Z0-9]$"))
{
    throw "Key vault name derrived from resource group has illegal characters: $keyVaultName";
}

########################################################################################################
# Generate self-signed certs and to use in DPS and IoT hub
# New certs will be generated each time you run the script as the script cleans up in the end
########################################################################################################

$subjectPrefix = "IoT Test";
$rootCommonName = "$subjectPrefix Root CA";
$intermediateCert1CommonName = "$subjectPrefix Intermediate 1 CA";
$intermediateCert2CommonName = "$subjectPrefix Intermediate 2 CA";

$rootCertPath = "$PSScriptRoot\Root.cer";
$intermediateCert1CertPath = "$PSScriptRoot\intermediateCert1.cer";
$intermediateCert2CertPath = "$PSScriptRoot\intermediateCert2.cer";
$intermediateCert2PfxPath = "$PSScriptRoot\intermediateCert2.pfx"
$verificationCertPath = "$PSScriptRoot\verification.cer";

$iotHubX509DeviceCertCommonName = "iothubx509device1";
$iotHubX509DevicePfxPath = "$PSScriptRoot\IotHubX509Device.pfx";
$iotHubX509CertChainDeviceCommonName = "iothubx509chaindevice1";
$iotHubX509ChainDevicPfxPath = "$PSScriptRoot\IotHubX509ChainDevice.pfx";

$dpsTrustBundleCertificateCommonName = "$subjectPrefix DPS Private Preview - Root CA";
$dpsTrustBundleCertificateCertPath = "$PSScriptRoot\dpsTrustBundleCertificate.cer";
$dpsTrustBundleCertificatePemPath = "$PSScriptRoot\dpsTrustBundleCertificate.pem";

############################################################################################################################
# Cleanup old certs and files that can cause a conflict
############################################################################################################################

CleanUp-Certs

# Generate self signed Root and Intermediate CA cert, expiring in 2 years
# These certs are used for signing so ensure to have the correct KeyUsage - CertSign and TestExtension - ca=TRUE&pathlength=12

Write-Host "`nGenerating self signed certs."

# Generate the certificates used by both IoT Hub and DPS tests.

# Create certificate chain from Root to Intermediate2.
# This chain will be combined with the certificates that are signed by Intermediate2 to test X509 CA-chained devices for IoT Hub and DPS (group enrollment) tests.
# Chain: Root->Intermediate1->Intermediate2, device cert: Intermediate2->deviceCert
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

Export-Certificate -cert $rootCACert -FilePath $rootCertPath -Type CERT | Out-Null
$x509ChainRootCACertBase64 = [Convert]::ToBase64String((Get-Content $rootCertPath -AsByteStream))

Export-Certificate -cert $intermediateCert1 -FilePath $intermediateCert1CertPath -Type CERT | Out-Null
$x509ChainIntermediate1CertBase64 = [Convert]::ToBase64String((Get-Content $intermediateCert1CertPath -AsByteStream));

Export-Certificate -cert $intermediateCert2 -FilePath $intermediateCert2CertPath -Type CERT | Out-Null
$x509ChainIntermediate2CertBase64 = [Convert]::ToBase64String((Get-Content $intermediateCert2CertPath -AsByteStream));

$certPassword = ConvertTo-SecureString $GroupCertificatePassword -AsPlainText -Force

# Export the intermediate2 certificate as a pfx file. This certificate will be used to sign and generate the device certificates that are used in DPS group enrollment E2E tests.
Export-PFXCertificate -cert $intermediateCert2 -filePath $intermediateCert2PfxPath -password $certPassword | Out-Null
$x509ChainIntermediate2PfxBase64 = [Convert]::ToBase64String((Get-Content $intermediateCert2PfxPath -AsByteStream));

# Generate the certificates used by only IoT Hub E2E tests.

# Generate an X509 self-signed certificate. This certificate will be used by test device identities that test X509 self-signed certificate device authentication.
# Leaf certificates are not used for signing so don't specify KeyUsage and TestExtension - ca=TRUE&pathlength=12
$iotHubX509SelfSignedDeviceCert = New-SelfSignedCertificate `
    -DnsName "$iotHubX509DeviceCertCommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -HashAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)

$iotHubCredentials = New-Object System.Management.Automation.PSCredential("Password", (New-Object System.Security.SecureString))
Export-PFXCertificate -cert $iotHubX509SelfSignedDeviceCert -filePath $iotHubX509DevicePfxPath -password $iotHubCredentials.Password | Out-Null
$iothubX509DevicePfxBase64 = [Convert]::ToBase64String((Get-Content $iotHubX509DevicePfxPath -AsByteStream));
$iothubX509DevicePfxThumbprint = $iotHubX509SelfSignedDeviceCert.Thumbprint

# Generate the leaf device certificate signed by Intermediate2. This certificate will be used by test device identities that test X509 CA-signed certificate device authentication.
# Leaf certificates are not used for signing so don't specify KeyUsage and TestExtension - ca=TRUE&pathlength=12
$iotHubX509ChainDeviceCert = New-SelfSignedCertificate `
    -DnsName "$iotHubX509CertChainDeviceCommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -HashAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $intermediateCert2

Export-PFXCertificate -cert $iotHubX509ChainDeviceCert -filePath $iotHubX509ChainDevicPfxPath -password $iotHubCredentials.Password | Out-Null
$iothubX509ChainDevicePfxBase64 = [Convert]::ToBase64String((Get-Content $iotHubX509ChainDevicPfxPath -AsByteStream));

# Generate the certificates used by only DPS E2E tests.

# Generate a self-signed test certificate. This certificate will be uploaded to DPS as a Trust Bundle and will be linked to the test enrollments.
# In production scenarios, this certificate shoould be replaced by the private root certificate downloaded from CA/PKI.
$trustBundleTestCertificate = New-SelfSignedCertificate `
    -DnsName "$dpsTrustBundleCertificateCommonName" `
    -KeyUsage CertSign `
    -TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -HashAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)

########################################################################################################
# Install latest version of az cli
########################################################################################################

if ($InstallDependencies)
{
    Write-Host "`nInstalling and updating AZ CLI."
    Install-Module -Name Az -AllowClobber -Force
    Update-Module -Name Az
}

Check-AzureCliVersion

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
# Configure an AAD app to authenticate Log Analytics Workspace, if specified
#################################################################################################################################################

if ($EnableIotHubSecuritySolution)
{
    Write-Host "`nCreating app registration $logAnalyticsAppRegnName"
    $logAnalyticsAppRegUrl = "http://$logAnalyticsAppRegnName"
    $logAnalyticsAppId = az ad sp create-for-rbac -n $logAnalyticsAppRegUrl --role "Reader" --scope $resourceGroupId --query "appId" --output tsv
    Write-Host "`nCreated application $logAnalyticsAppRegnName with Id $logAnalyticsAppId."
}

#################################################################################################################################################
# Configure an AAD app and assign it contributor role to perform IoT hub data actions.
#################################################################################################################################################

Write-Host "`nCreating app registration $e2eTestAadAppRegName for IoT hub data actions"
$iotHubDataContributorRoleId = "4fc6c259987e4a07842ec321cc9d413f"
$iotHubScope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Devices/IotHubs/$iotHubName"
$e2eTestAadAppInfo = az ad sp create-for-rbac -n $e2eTestAadAppRegName --role $iotHubDataContributorRoleId --scope $iotHubScope --query '{appId:appId, password:password}' | ConvertFrom-Json

$e2eTestAadAppId = $e2eTestAadAppInfo.appId
$e2eTestAadAppPassword = $e2eTestAadAppInfo.password
Write-Host "`nCreated application $e2eTestAadAppRegName with Id $e2eTestAadAppId."

#################################################################################################################################################
# Configure the above created AAD app to perform DPS data actions.
#################################################################################################################################################

Write-Host "`nGiving app registration $e2eTestAadAppRegName data contributor permission on DPS instance $dpsName"
$dpsContributorId = "dfce44e4-17b7-4bd1-a6d1-04996ec95633"
$dpsScope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Devices/ProvisioningServices/$dpsName"
az role assignment create --role $dpsContributorId --assignee $e2eTestAadAppId --scope $dpsScope

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

$certExists = az iot hub certificate list -g $ResourceGroup --hub-name $iotHubName --query "value[?name=='$hubUploadCertificateName']" --output tsv
if ($certExists)
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

$iotHubCertChainDevice = az iot hub device-identity list -g $ResourceGroup --hub-name $iotHubName --query "[?deviceId=='$iotHubX509CertChainDeviceCommonName'].deviceId" --output tsv

if (-not $iotHubCertChainDevice)
{
    Write-Host "`nCreating X509 CA certificate authenticated device $iotHubX509CertChainDeviceCommonName on IoT hub."
    az iot hub device-identity create -g $ResourceGroup --hub-name $iotHubName --device-id $iotHubX509CertChainDeviceCommonName --am x509_ca
}

##################################################################################################################################
# Uploading certificate to DPS, verifying, and creating enrollment groups
##################################################################################################################################

$dpsIdScope = az iot dps show -g $ResourceGroup --name $dpsName --query 'properties.idScope' --output tsv
$certExists = az iot dps certificate list -g $ResourceGroup --dps-name $dpsName --query "value[?name=='$dpsUploadCertificateName']" --output tsv
if ($certExists)
{
    Write-Host "`nDeleting existing certificate from DPS."
    $etag = az iot dps certificate show -g $ResourceGroup --dps-name $dpsName --certificate-name $dpsUploadCertificateName --query 'etag'
    az iot dps certificate delete -g $ResourceGroup --dps-name $dpsName --name $dpsUploadCertificateName --etag $etag
}
Write-Host "`nUploading new certificate to DPS."
az iot dps certificate create -g $ResourceGroup --path $rootCertPath --dps-name $dpsName --certificate-name $dpsUploadCertificateName --output none

$isVerified = az iot dps certificate show -g $ResourceGroup --dps-name $dpsName --certificate-name $dpsUploadCertificateName --query 'properties.isVerified' --output tsv
if ($isVerified -eq 'false')
{
    Write-Host "`nVerifying certificate uploaded to DPS."
    $etag = az iot dps certificate show -g $ResourceGroup --dps-name $dpsName --certificate-name $dpsUploadCertificateName --query 'etag'
    $requestedCommonName = az iot dps certificate generate-verification-code -g $ResourceGroup --dps-name $dpsName --certificate-name $dpsUploadCertificateName -e $etag --query 'properties.verificationCode'
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
    $etag = az iot dps certificate show -g $ResourceGroup --dps-name $dpsName --certificate-name $dpsUploadCertificateName --query 'etag'
    az iot dps certificate verify -g $ResourceGroup --dps-name $dpsName --certificate-name $dpsUploadCertificateName -e $etag --path $verificationCertPath --output none
}

$groupEnrollmentId = "Group1"
$groupEnrollmentExists = az iot dps enrollment-group list -g $ResourceGroup --dps-name $dpsName --query "[?enrollmentGroupId=='$groupEnrollmentId'].enrollmentGroupId" --output tsv
if ($groupEnrollmentExists)
{
    Write-Host "`nDeleting existing group enrollment $groupEnrollmentId."
    az iot dps enrollment-group delete -g $ResourceGroup --dps-name $dpsName --enrollment-id $groupEnrollmentId
}
Write-Host "`nAdding group enrollment $groupEnrollmentId."
az iot dps enrollment-group create -g $ResourceGroup --dps-name $dpsName --enrollment-id $groupEnrollmentId --ca-name $dpsUploadCertificateName --output none

#################################################################################################################################################
# Link your DPS instance to your certificate authority which can accept client certificate signing requests and issue certificates.
#################################################################################################################################################

# Note: This feature is currently in private preview. In order to use this feature you will first need to get your DPS instance added to the allow-list.
# For more details, see https://github.com/Azure/CertsForIoT-B#getting-started.

# Azure CLI support is currently unavailable for linking DPS instance to certificate authority.
# The powershell command below will need to be replaced by Azure CLI once the support is available.

if ([string]::IsNullOrEmpty($CertificateAuthorityProfileId) -or [string]::IsNullOrEmpty($CertificateAuthorityApiKey))
{
    Write-Host "`nCertificate Authority details not provided for DPS client certificate issuance. Skipping this step."
}
else
{
    Write-Host "DPS service API version $dpsServiceApiPreviewVersion is currently in private-preview. Ensure that your DPS instance $dpsName is in the allow-list, else the operation will fail."

    $dpsPrimaryKey =  az iot dps policy show --dps-name $dpsName --resource-group $ResourceGroup --policy-name provisioningserviceowner --query primaryKey --output tsv
    $dpsEndpoint = az iot dps show --name $dpsName --query properties.serviceOperationsHostName --output tsv

    $dpsKeyName = "provisioningserviceowner"

    $dpsServiceApiSasToken = Calculate-SasKey $dpsKeyName $dpsPrimaryKey $dpsEndpoint 3600

    Write-Host "`nLinking DPS host $dpsName to your DigiCert certificate authority with friendly name $dpsCaName."

    $uriRequest = [System.UriBuilder]"https://$dpsEndpoint/certificateAuthorities/$dpsCaName"

    $uriQueryCollection = [System.Web.HttpUtility]::ParseQueryString([String]::Empty)
    $uriQueryCollection.Add("api-version", $dpsServiceApiPreviewVersion)

    $uriRequest.Query = $uriQueryCollection.ToString()

    $headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
    $headers.Add("Authorization", $dpsServiceApiSasToken)
    $headers.Add("Content-Type", "application/json")

    $body = @{
        'certificateAuthorityType' = "DigiCertCertificateAuthority"
        'profileName' = "$CertificateAuthorityProfileId"
        'apiKey' = "$CertificateAuthorityApiKey"
    }
    $jsonBody = $body | ConvertTo-Json

    Invoke-RestMethod -Uri $uriRequest.Uri -Method "PUT" -Headers $headers -Body $jsonBody
}

#################################################################################################################################################
# Link your DPS instance to your Trust Bundle certificate. The IoT Edge TLS server certificate is chained up to a certificate
# in the Trust Bundle.
#################################################################################################################################################

# Note: This feature is currently in private preview. In order to use this feature you will first need to get your DPS instance added to the allow-list.
# For more details, see https://github.com/Azure/CertsForIoT-B#getting-started.

# Azure CLI support is currently unavailable for linking DPS instance to Trust Bundle.
# The powershell command below will need to be replaced by Azure CLI once the support is available.

Write-Host "DPS service API version $dpsServiceApiPreviewVersion is currently in private-preview. Ensure that your DPS instance $dpsName is in the allow-list, else the operation will fail."

Write-Host "`nLinking DPS host $dpsName to your Test Trust Bundle with friendly name $dpsTrustBundleId."

$uriRequest = [System.UriBuilder]"https://$dpsEndpoint/trustBundles/$dpsTrustBundleId"

$uriQueryCollection = [System.Web.HttpUtility]::ParseQueryString([String]::Empty)
$uriQueryCollection.Add("api-version", $dpsServiceApiPreviewVersion)

$uriRequest.Query = $uriQueryCollection.ToString()

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add("Authorization", $dpsServiceApiSasToken)
$headers.Add("Content-Type", "application/json")

Export-Certificate -cert $trustBundleTestCertificate -filePath $dpsTrustBundleCertificateCertPath -Type Cert | Out-Null
certutil -encode $dpsTrustBundleCertificateCertPath $dpsTrustBundleCertificatePemPath | Out-Null

$certificateData = Get-Content ($dpsTrustBundleCertificatePemPath) -Raw
$jsonBody = "{'certificates': [{'certificate': '$certificateData'}]}"

Invoke-RestMethod -Uri $uriRequest.Uri -Method "PUT" -Headers $headers -Body $jsonBody

##################################################################################################################################
#Enable Azure Security Solutions, if specified
##################################################################################################################################

if ($EnableIotHubSecuritySolution)
{
    Write-Host "`nCreating a self-signed certificate for LA and placing it in $ResourceGroup."
    az ad app credential reset --id $logAnalyticsAppId --create-cert --keyvault $keyVaultName --cert $ResourceGroup --output none
    Write-Host "`nSuccessfully created a self signed certificate for your application $logAnalyticsAppRegnName in $keyVaultName key vault with cert name $ResourceGroup."

    Write-Host "`nFetching the certificate binary for LA."
    $selfSignedCerts = "$PSScriptRoot\selfSignedCerts"
    if (Test-Path $selfSignedCerts -PathType Leaf)
    {
        Remove-Item -r $selfSignedCerts
    }

    az keyvault secret download --file $selfSignedCerts --vault-name $keyVaultName -n $ResourceGroup --encoding base64
    $fileContent = Get-Content $selfSignedCerts -AsByteStream
    $fileContentB64String = [System.Convert]::ToBase64String($fileContent);

    Write-Host "`nSuccessfully fetched the certificate bytes for LA. Removing the cert file from the disk."
    Remove-Item -r $selfSignedCerts
}

##################################################################################################################################
# Create the IoT devices and modules that are used by the .NET samples
##################################################################################################################################

if ($GenerateResourcesForSamplesDevOpsPipeline)
{
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
}

##################################################################################################################################
# Create the DPS enrollments that are used by the .NET samples
##################################################################################################################################

if ($GenerateResourcesForSamplesDevOpsPipeline)
{
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
}

###################################################################################################################################
# Store all secrets in a KeyVault - Values will be pulled down from here to configure environment variables
###################################################################################################################################

$dpsEndpoint = "global.azure-devices-provisioning.net"
if ($Region.EndsWith('euap', 'CurrentCultureIgnoreCase'))
{
    $dpsEndpoint = "global-canary.azure-devices-provisioning.net"
}

# This variable will be overwritten in the yaml file depending on the OS setup of the test environment.
# This variable is set here to help run local E2E tests using docker-based proxy setup.
$proxyServerAddress = "127.0.0.1:8888"

$keyvaultKvps = @{
    # Environment variables for IoT Hub E2E tests
    "IOTHUB-CONNECTION-STRING" = $iotHubConnectionString;
    "IOTHUB-X509-DEVICE-PFX-CERTIFICATE" = $iothubX509DevicePfxBase64;
    "IOTHUB-X509-CHAIN-DEVICE-NAME" = $iotHubX509CertChainDeviceCommonName;
    "IOTHUB-X509-CHAIN-DEVICE-PFX-CERTIFICATE" = $iothubX509ChainDevicePfxBase64;
    "IOTHUB-USER-ASSIGNED-MSI-RESOURCE-ID" = $msiResourceId;

    # Environment variables for DPS E2E tests
    "DPS-IDSCOPE" = $dpsIdScope;
    "PROVISIONING-CONNECTION-STRING" = $dpsConnectionString;
    "DPS-GLOBALDEVICEENDPOINT" = $dpsEndpoint;
    "FAR-AWAY-IOTHUB-HOSTNAME" = $farHubHostName;
    "CUSTOM-ALLOCATION-POLICY-WEBHOOK" = $customAllocationPolicyWebhook;
    "DPS-X509-PFX-CERTIFICATE-PASSWORD" = $GroupCertificatePassword;
    "DPS-X509-GROUP-ENROLLMENT-NAME" = $groupEnrollmentId;
    "CA-NAME" = $dpsCaName;
    "TRUST-BUNDLE-ID" = $dpsTrustBundleId;

    # Environment variables for Azure resources used for E2E tests (common)
    "X509-CHAIN-ROOT-CA-CERTIFICATE" = $x509ChainRootCACertBase64;
    "X509-CHAIN-INTERMEDIATE1-CERTIFICATE" = $x509ChainIntermediate1CertBase64;
    "X509-CHAIN-INTERMEDIATE2-CERTIFICATE" = $x509ChainIntermediate2CertBase64;
    "X509-CHAIN-INTERMEDIATE2-PFX-CERTIFICATE" = $x509ChainIntermediate2PfxBase64;
    "STORAGE-ACCOUNT-CONNECTION-STRING" = $storageAccountConnectionString;
    "MSFT-TENANT-ID" = "72f988bf-86f1-41af-91ab-2d7cd011db47";
    "E2E-TEST-AAD-APP-CLIENT-ID" = $e2eTestAadAppId;
    "E2E-TEST-AAD-APP-CLIENT-SECRET" = $e2eTestAadAppPassword;
    "E2E-IKEY" = $instrumentationKey;

    # Environment variables for the DevOps pipeline
    "PIPELINE-ENVIRONMENT" = "prod";
    "PROXY-SERVER-ADDRESS" = $proxyServerAddress;

    # Environment variables for invalid certificate tests
    # The connection strings below point to servers with incorrect TLS server certificates. Tests will attempt to connect and expect that the TLS connection ends in a security exception.
    <#[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="fake shared access token")]#>
    "IOTHUB-DEVICE-CONN-STRING-INVALIDCERT" = "HostName=invalidcertiothub1.westus.cloudapp.azure.com;DeviceId=DoNotDelete1;SharedAccessKey=zWmeTGWmjcgDG1dpuSCVjc5ZY4TqVnKso5+g1wt/K3E=";
    <#[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="fake shared access token")]#>
    "IOTHUB-CONN-STRING-INVALIDCERT" = "HostName=invalidcertiothub1.westus.cloudapp.azure.com;SharedAccessKeyName=iothubowner;SharedAccessKey=Fk1H0asPeeAwlRkUMTybJasksTYTd13cgI7SsteB05U=";
    "DPS-GLOBALDEVICEENDPOINT-INVALIDCERT" = "invalidcertgde1.westus.cloudapp.azure.com";
    <#[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="fake shared access token")]#>
    "PROVISIONING-CONNECTION-STRING-INVALIDCERT" = "HostName=invalidcertdps1.westus.cloudapp.azure.com;SharedAccessKeyName=provisioningserviceowner;SharedAccessKey=lGO7OlXNhXlFyYV1rh9F/lUCQC1Owuh5f/1P0I1AFSY=";

    # These environment variables are only used in Java
    "IOT-DPS-CONNECTION-STRING" = $dpsConnectionString; # DPS Connection string Environment variable for Java
    "IOT-DPS-ID-SCOPE" = $dpsIdScope; # DPS ID Scope Environment variable for Java
    "FAR-AWAY-IOTHUB-CONNECTION-STRING" = $farHubConnectionString;
    "IS-BASIC-TIER-HUB" = "false";
}

# Environment variables used by .NET samples
if ($GenerateResourcesForSamplesDevOpsPipeline)
{
    $keyvaultKvps.Add("IOTHUB-DEVICE-CONN-STRING", $iotHubSasBasedDeviceConnectionString)
    $keyvaultKvps.Add("IOTHUB-MODULE-CONN-STRING", $iotHubSasBasedModuleConnectionString)
    $keyvaultKvps.Add("PNP-TC-DEVICE-CONN-STRING", $temperatureControllerSampleDeviceConnectionString)
    $keyvaultKvps.Add("PNP-THERMOSTAT-DEVICE-CONN-STRING", $thermostatSampleDeviceConnectionString)
    $keyvaultKvps.Add("IOTHUB-X509-DEVICE-PFX-THUMBPRINT", $iothubX509DevicePfxThumbprint)
    $keyvaultKvps.Add("IOTHUB-SAS-KEY", $iothubownerSasPrimaryKey)
    $keyvaultKvps.Add("IOTHUB-SAS-KEY-NAME", $iothubownerSasPolicy)
    $keyvaultKvps.Add("DPS-SYMMETRIC-KEY-INDIVIDUAL-ENROLLMENT-REGISTRATION-ID", $symmetricKeySampleEnrollmentRegistrationId)
    $keyvaultKvps.Add("DPS-SYMMETRIC-KEY-INDIVIDUAL-ENROLLEMNT-PRIMARY-KEY", $symmetricKeySampleEnrollmentPrimaryKey)
}

# Environment variables used by Log Analytics Workspace for Azure Security Center
if ($EnableIotHubSecuritySolution)
{
    $keyvaultKvps.Add("LA-WORKSPACE-ID", $workspaceId)
    $keyvaultKvps.Add("LA-AAD-APP-CERT-BASE64", $fileContentB64String)
    $keyvaultKvps.Add("LA-AAD-APP-ID", $logAnalyticsAppId)
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
# Notify user that openssl is required for running E2E tests
# openssl is currently unavailable as an official release via Chocolatey, so it is advised to perform a manual install from a secured source.
############################################################################################################################

Write-Host "Ensure that openssl is available on your system and is set to PATH variable."
Write-Host "If you have Git installed, openssl can be found at `"<Git_install_directory>\Git\usr\bin\openssl.exe`""

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