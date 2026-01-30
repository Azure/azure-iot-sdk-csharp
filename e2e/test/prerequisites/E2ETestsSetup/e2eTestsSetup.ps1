# NOTE: This script needs to be run using admin mode

param(
    [Parameter(Mandatory)]
    [string] $Region,

    [Parameter(Mandatory)]
    [string] $ResourceGroup,
    
    [Parameter(Mandatory)]
    [string] $SubscriptionId,

    [Parameter(Mandatory)]
    [string] $GroupCertificatePassword
)

$startTime = (Get-Date)

########################################################################################################
# Set error and warning preferences for the script to run.
########################################################################################################

$ErrorActionPreference = "Stop"
$WarningActionPreference = "Continue"

########################################################################################################
# Check PowerShell version
########################################################################################################
if ($PSversiontable.PSVersion -lt "7.0.0")
{
    Write-Error "This script requires PowerShell v7. Please install it and rerun."
    exit
}

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


#################################################################################################
# Set required parameters.
#################################################################################################

$Region = $Region.Replace(' ', '')
$dpsUploadCertificateName = "group1-certificate"
$hubUploadCertificateName = "rootCA"
$iothubUnitsToBeCreated = 5;
$managedIdentityName = "$ResourceGroup-user-msi"

# OpenSSL has dropped support for SHA1 signed certificates in Ubuntu 20.04, so our test resources will use SHA256 signed certificates instead.
$certificateHashAlgorithm = "SHA256"

#################################################################################################
# Get Function App contents to pass to deployment
#################################################################################################

## remove any characters that aren't letters or numbers, and then validate
$storageAccountName = "$($ResourceGroup.ToLower())sa"
$storageAccountName = [regex]::Replace($storageAccountName, "[^a-z0-9]", "")
if (-not ($storageAccountName -match "^[a-z0-9][a-z0-9]{1,22}[a-z0-9]$"))
{
    throw "Storage account name derived from resource group has illegal characters: $storageAccountName"
}

########################################################################################################
# Generate self-signed certs and to use in DPS and IoT hub.
# New certs will be generated each time you run the script as the script cleans up in the end.
########################################################################################################

$subjectPrefix = "IoT Test";
$rootCommonName = "$subjectPrefix Root CA";
$intermediateCert1CommonName = "$subjectPrefix Intermediate 1 CA";
$intermediateCert2CommonName = "$subjectPrefix Intermediate 2 CA";

$rootCertPath = "$PSScriptRoot/Root.cer";
$intermediateCert1CertPath = "$PSScriptRoot/intermediateCert1.cer";
$intermediateCert2CertPath = "$PSScriptRoot/intermediateCert2.cer";
$intermediateCert2PfxPath = "$PSScriptRoot/intermediateCert2.pfx"
$verificationCertPath = "$PSScriptRoot/verification.cer";

$iotHubX509DeviceCertCommonName = "Save_iothubx509device1";
$iotHubX509DevicePfxPath = "$PSScriptRoot/IotHubX509Device.pfx";
$iotHubX509CertChainDeviceCommonName = "Save_iothubx509chaindevice1";
$iotHubX509ChainDevicPfxPath = "$PSScriptRoot/IotHubX509ChainDevice.pfx";

# Generate self signed Root and Intermediate CA cert, expiring in 2 years
# These certs are used for signing so ensure to have the correct KeyUsage - CertSign and TestExtension - ca=TRUE&pathlength=12

# This module is required to use commands like New-SelfSignedCertificate and is not installed on ADO's linux powershell
# by default
Install-Module -Name PSPKI -RequiredVersion 3.7.2 -Scope CurrentUser -Force
Import-Module -Name PSPKI

Write-Host "`nGenerating self signed certs."

# Generate the certificates used by both IoT Hub and DPS tests.

# Create certificate chain from Root to Intermediate2.
# This chain will be combined with the certificates that are signed by Intermediate2 to test X509 CA-chained devices for IoT Hub and DPS (group enrollment) tests.
# Chain: Root->Intermediate1->Intermediate2, device cert: Intermediate2->deviceCert
$rootCACert = New-SelfSignedCertificateEx `
    -Subject "CN=$rootCommonName" `
    -KeyUsage 4 ` # KeyCertSign
    #-TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -SignatureAlgorithm "$certificateHashAlgorithm" `
    -StoreLocation 2 ` # LocalMachine
    -NotAfter (Get-Date).AddYears(2)

$intermediateCert1 = New-SelfSignedCertificateEx `
    -Subject  "CN=$intermediateCert1CommonName" `
    -KeyUsage 4 ` # KeyCertSign
    #-TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -SignatureAlgorithm "$certificateHashAlgorithm" `
    -StoreLocation 2 ` # LocalMachine
    -NotAfter (Get-Date).AddYears(2) `
    -Issuer $rootCACert


$intermediateCert2 = New-SelfSignedCertificateEx `
    -Subject  "CN=$intermediateCert2CommonName" `
    -KeyUsage 4 ` # KeyCertSign
    #-TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -SignatureAlgorithm "$certificateHashAlgorithm" `
    -StoreLocation 2 ` # LocalMachine
    -NotAfter (Get-Date).AddYears(2) `
    -Issuer $intermediateCert1

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
$iotHubX509SelfSignedDeviceCert = New-SelfSignedCertificateEx `
    -Subject "CN=$iotHubX509DeviceCertCommonName" `
    -KeySpec Signature `
    #-TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -SignatureAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)

$iotHubCredentials = New-Object System.Management.Automation.PSCredential("Password", (New-Object System.Security.SecureString))
Export-PFXCertificate -cert $iotHubX509SelfSignedDeviceCert -filePath $iotHubX509DevicePfxPath -password $iotHubCredentials.Password | Out-Null
$iothubX509DevicePfxBase64 = [Convert]::ToBase64String((Get-Content $iotHubX509DevicePfxPath -AsByteStream));
$iothubX509DevicePfxThumbprint = $iotHubX509SelfSignedDeviceCert.Thumbprint

# Generate the leaf device certificate signed by Intermediate2. This certificate will be used by test device identities that test X509 CA-signed certificate device authentication.
# Leaf certificates are not used for signing so don't specify KeyUsage and TestExtension - ca=TRUE&pathlength=12
$iotHubX509ChainDeviceCert = New-SelfSignedCertificateEx `
    -Subject "CN=$iotHubX509CertChainDeviceCommonName" `
    -KeySpec Signature `
    #-TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -SignatureAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Issuer $intermediateCert2

Export-PFXCertificate -cert $iotHubX509ChainDeviceCert -filePath $iotHubX509ChainDevicPfxPath -password $iotHubCredentials.Password | Out-Null
$iothubX509ChainDevicePfxBase64 = [Convert]::ToBase64String((Get-Content $iotHubX509ChainDevicPfxPath -AsByteStream));

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
# Invoke-Deployment - Uses the .\.json template to create the necessary resources to run E2E tests.
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
    StorageAccountName=$storageAccountName `
    HubUnitsCount=$iothubUnitsToBeCreated `
    UserAssignedManagedIdentityName=$managedIdentityName `
    EnableIotHubSecuritySolution=$EnableIotHubSecuritySolution

if ($LastExitCode -ne 0)
{
    throw "Error running resource group deployment."
}

Write-Host "`nYour infrastructure is ready in subscription ($SubscriptionId), resource group ($ResourceGroup)."

#########################################################################################################
# Get properties to setup the config file for environment variables.
#########################################################################################################

Write-Host "`nGetting generated names and secrets from ARM template output."
$iotHubConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.hubConnectionString.value' --output tsv
$dpsName = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.dpsName.value' --output tsv
$dpsConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName  --query 'properties.outputs.dpsConnectionString.value' --output tsv
$storageAccountConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName  --query 'properties.outputs.storageAccountConnectionString.value' --output tsv
$workspaceId = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.workspaceId.value' --output tsv
$iotHubName = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.hubName.value' --output tsv

#################################################################################################################################################
# Configure an AAD app and assign it contributor role to perform IoT hub data actions.
#################################################################################################################################################

#Write-Host "`nCreating app registration $e2eTestAadAppRegName for IoT hub data actions"
#$iotHubDataContributorRoleId = "4fc6c259987e4a07842ec321cc9d413f"
#$iotHubScope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Devices/IotHubs/$iotHubName"
#$e2eTestAadAppInfo = az ad sp create-for-rbac -n $e2eTestAadAppRegName --role $iotHubDataContributorRoleId --scope $iotHubScope --query '{appId:appId, password:password}' | ConvertFrom-Json

#$e2eTestAadAppId = $e2eTestAadAppInfo.appId
#$e2eTestAadAppPassword = $e2eTestAadAppInfo.password
#Write-Host "`nCreated application $e2eTestAadAppRegName with Id $e2eTestAadAppId."

#################################################################################################################################################
# Configure the above created AAD app to perform DPS data actions.
#################################################################################################################################################

#Write-Host "`nGiving app registration $e2eTestAadAppRegName data contributor permission on DPS instance $dpsName"
#$dpsContributorId = "dfce44e4-17b7-4bd1-a6d1-04996ec95633"
#$dpsScope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Devices/ProvisioningServices/$dpsName"
#az role assignment create --role $dpsContributorId --assignee $e2eTestAadAppId --scope $dpsScope

#################################################################################################################################################
# Add role assignement for User assinged managed identity to be able to perform import and export jobs on the IoT hub.
#################################################################################################################################################

#Write-Host "`nGranting the user assigned managed identity $managedIdentityName Storage Blob Data Contributor permissions on resource group: $ResourceGroup."
#$msiPrincipalId = az identity show -n $managedIdentityName -g $ResourceGroup --query principalId --output tsv
#$msiResourceId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.ManagedIdentity/userAssignedIdentities/$managedIdentityName"
#az role assignment create --assignee $msiPrincipalId --role 'Storage Blob Data Contributor' --scope $resourceGroupId --output none

##################################################################################################################################
# Granting the IoT hub system identity storage blob contributor access on the resoruce group.
##################################################################################################################################

#Write-Host "`nGranting the system identity on the hub $iotHubName Storage Blob Data Contributor permissions on resource group: $ResourceGroup."
#$systemIdentityPrincipal = az resource list -n $iotHubName --query [0].identity.principalId --out tsv
#az role assignment create --assignee $systemIdentityPrincipal --role "Storage Blob Data Contributor" --scope $resourceGroupId --output none

##################################################################################################################################
# Uploading root CA certificate to IoT hub and verifying.
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
        "-Subject"                       = "CN=$requestedCommonName";
        "-StoreLocation"                 = 2;
        "-NotAfter"                      = (get-date).AddYears(2);
        "-TextExtension"                 = @("2.5.29.37={text}1.3.6.1.5.5.7.3.2,1.3.6.1.5.5.7.3.1", "2.5.29.19={text}ca=FALSE&pathlength=0");
        "-SignatureAlgorithm"            = $certificateHashAlgorithm;
        "-Issuer"                        = $rootCACert;
    }
    $verificationCert = New-SelfSignedCertificateEx @verificationCertArgs
    Export-Certificate -cert $verificationCert -filePath $verificationCertPath -Type Cert | Out-Null
    $etag = az iot hub certificate show -g $ResourceGroup --hub-name $iotHubName --name $hubUploadCertificateName --query 'etag'
    az iot hub certificate verify -g $ResourceGroup --hub-name $iotHubName --name $hubUploadCertificateName -e $etag --path $verificationCertPath --output none
}

##################################################################################################################################
# Fetch the iothubowner policy details.
##################################################################################################################################

$iothubownerSasPolicy = "iothubowner"
$iothubownerSasPrimaryKey = az iot hub policy show --hub-name $iotHubName --name $iothubownerSasPolicy --query 'primaryKey'

##################################################################################################################################
# Create device in IoT hub that uses a certificate signed by intermediate certificate.
##################################################################################################################################

$iotHubCertChainDevice = az iot hub device-identity list -g $ResourceGroup --hub-name $iotHubName --query "[?deviceId=='$iotHubX509CertChainDeviceCommonName'].deviceId" --output tsv

if (-not $iotHubCertChainDevice)
{
    Write-Host "`nCreating X509 CA certificate authenticated device $iotHubX509CertChainDeviceCommonName on IoT hub."
    az iot hub device-identity create -g $ResourceGroup --hub-name $iotHubName --device-id $iotHubX509CertChainDeviceCommonName --am x509_ca
}

##################################################################################################################################
# Uploading certificate to DPS, verifying, and creating enrollment groups.
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
        "-Issuer"              = $rootCACert;
    }
    $verificationCert = New-SelfSignedCertificateEx @verificationCertArgs
    Export-Certificate -cert $verificationCert -filePath $verificationCertPath -Type Cert | Out-Null
    $etag = az iot dps certificate show -g $ResourceGroup --dps-name $dpsName --certificate-name $dpsUploadCertificateName --query 'etag'
    az iot dps certificate verify -g $ResourceGroup --dps-name $dpsName --certificate-name $dpsUploadCertificateName -e $etag --path $verificationCertPath --output none
}

$groupEnrollmentId = "Save_Group1"
$groupEnrollmentExists = az iot dps enrollment-group list -g $ResourceGroup --dps-name $dpsName --query "[?enrollmentGroupId=='$groupEnrollmentId'].enrollmentGroupId" --output tsv
if ($groupEnrollmentExists)
{
    Write-Host "`nDeleting existing group enrollment $groupEnrollmentId."
    az iot dps enrollment-group delete -g $ResourceGroup --dps-name $dpsName --enrollment-id $groupEnrollmentId
}
Write-Host "`nAdding group enrollment $groupEnrollmentId."
az iot dps enrollment-group create -g $ResourceGroup --dps-name $dpsName --enrollment-id $groupEnrollmentId --ca-name $dpsUploadCertificateName --output none

###################################################################################################################################
# Configure environment variables with secret values.
###################################################################################################################################

$dpsEndpoint = "global.azure-devices-provisioning.net"
if ($Region.EndsWith('euap', 'CurrentCultureIgnoreCase'))
{
    $dpsEndpoint = "global-canary.azure-devices-provisioning.net"
}

# This variable will be overwritten in the yaml file depending on the OS setup of the test environment.
# This variable is set here to help run local E2E tests using docker-based proxy setup.
#$proxyServerAddress = "127.0.0.1:8888"

# Environment variables for IoT Hub E2E tests
Write-Host "##vso[task.setvariable variable=IOTHUB_CONNECTION_STRING;isOutput=false]$iotHubConnectionString"
Write-Host "##vso[task.setvariable variable=IOTHUB_X509_DEVICE_PFX_CERTIFICATE;isOutput=false]$iothubX509DevicePfxBase64"
Write-Host "##vso[task.setvariable variable=IOTHUB_X509_CHAIN_DEVICE_NAME;isOutput=false]$iotHubX509CertChainDeviceCommonName";
Write-Host "##vso[task.setvariable variable=IOTHUB_X509_CHAIN_DEVICE_PFX_CERTIFICATE;isOutput=false]$iothubX509ChainDevicePfxBase64"
Write-Host "##vso[task.setvariable variable=IOTHUB_USER_ASSIGNED_MSI_RESOURCE_ID;isOutput=false]$msiResourceId"

# Environment variables for DPS E2E tests
Write-Host "##vso[task.setvariable variable=DPS_IDSCOPE;isOutput=false]$dpsIdScope"
Write-Host "##vso[task.setvariable variable=PROVISIONING_CONNECTION_STRING;isOutput=false]$dpsConnectionString"
Write-Host "##vso[task.setvariable variable=DPS_GLOBALDEVICEENDPOINT;isOutput=false]$dpsEndpoint"
Write-Host "##vso[task.setvariable variable=DPS_X509_PFX_CERTIFICATE_PASSWORD;isOutput=false]$GroupCertificatePassword"
Write-Host "##vso[task.setvariable variable=DPS_X509_GROUP_ENROLLMENT_NAME;isOutput=false]$groupEnrollmentId"

# Environment variables for Azure resources used for E2E tests (common)
Write-Host "##vso[task.setvariable variable=X509_CHAIN_ROOT_CA_CERTIFICATE;isOutput=false]$x509ChainRootCACertBase64"
Write-Host "##vso[task.setvariable variable=X509_CHAIN_INTERMEDIATE1_CERTIFICATE;isOutput=false]$x509ChainIntermediate1CertBase64"
Write-Host "##vso[task.setvariable variable=X509_CHAIN_INTERMEDIATE2_CERTIFICATE;isOutput=false]$x509ChainIntermediate2CertBase64"
Write-Host "##vso[task.setvariable variable=X509_CHAIN_INTERMEDIATE2_PFX_CERTIFICATE;isOutput=false]$x509ChainIntermediate2PfxBase64"
Write-Host "##vso[task.setvariable variable=STORAGE_ACCOUNT_CONNECTION_STRING;isOutput=false]$storageAccountConnectionString"
Write-Host "##vso[task.setvariable variable=MSFT_TENANT_ID;isOutput=false]72f988bf_86f1_41af_91ab_2d7cd011db47"
Write-Host "##vso[task.setvariable variable=E2E_TEST_AAD_APP_CLIENT_ID;isOutput=false]$e2eTestAadAppId"
Write-Host "##vso[task.setvariable variable=E2E_TEST_AAD_APP_CLIENT_SECRET;isOutput=false]$e2eTestAadAppPassword"

# Environment variables for the DevOps pipeline
Write-Host "##vso[task.setvariable variable=PIPELINE_ENVIRONMENT;isOutput=false]prod";
Write-Host "##vso[task.setvariable variable=PROXY_SERVER_ADDRESS;isOutput=false]$proxyServerAddress";

# Environment variables for invalid certificate tests
# The connection strings below point to servers with incorrect TLS server certificates. Tests will attempt to connect and expect that the TLS connection ends in a security exception.
<#[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="fake shared access token")]#>
Write-Host "##vso[task.setvariable variable=IOTHUB_DEVICE_CONN_STRING_INVALIDCERT;isOutput=false]HostName=invalidcertiothub1.westus.cloudapp.azure.com;DeviceId=DoNotDelete1;SharedAccessKey=zWmeTGWmjcgDG1dpuSCVjc5ZY4TqVnKso5+g1wt/K3E="
<#[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="fake shared access token")]#>
Write-Host "##vso[task.setvariable variable=IOTHUB_CONN_STRING_INVALIDCERT;isOutput=false]HostName=invalidcertiothub1.westus.cloudapp.azure.com;SharedAccessKeyName=iothubowner;SharedAccessKey=Fk1H0asPeeAwlRkUMTybJasksTYTd13cgI7SsteB05U="
Write-Host "##vso[task.setvariable variable=DPS_GLOBALDEVICEENDPOINT_INVALIDCERT;isOutput=false]invalidcertgde1.westus.cloudapp.azure.com"
<#[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="fake shared access token")]#>
Write-Host "##vso[task.setvariable variable=PROVISIONING_CONNECTION_STRING_INVALIDCERT;isOutput=false]HostName=invalidcertdps1.westus.cloudapp.azure.com;SharedAccessKeyName=provisioningserviceowner;SharedAccessKey=lGO7OlXNhXlFyYV1rh9F/lUCQC1Owuh5f/1P0I1AFSY="


############################################################################################################################
# Notify user that openssl is required for running E2E tests.
# openssl is currently unavailable as an official release via Chocolatey, so it is advised to perform a manual install from a secured source.
############################################################################################################################

try
{
    Get-Command openssl.exe
}
catch
{
    Write-Host -ForegroundColor Red "E2E tests require openssl to be installed on your system and set to PATH variable."
    Write-Host -ForegroundColor Red "If you have Git installed, openssl can be found at `"<Git_install_directory>\Git\usr\bin\openssl.exe`"."
}

############################################################################################################################
# Clean up certs and files created by the script
############################################################################################################################

Write-Host "Done!"