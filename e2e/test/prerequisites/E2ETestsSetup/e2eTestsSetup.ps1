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

    # Provide the webhook link
    [Parameter(Mandatory)]
    [string] $CustomAllocationPolicyWebHook,

    # Set this to true on the first execution to get everything installed in poweshell. Does not need to be run everytime.
    [Parameter()]
    [bool] $InstallDependencies = $true
)

$startTime = (Get-Date)

########################################################################################################
# Set error and warning preferences for the script to run
########################################################################################################

$ErrorActionPreference = "Stop"
$WarningActionPreference = "Continue"

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
        Write-Host "`nPlease login to Azure..."
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
    Write-Host("`nCleaning up old certs and files that may cause conflicts.")
    Get-ChildItem "Cert:\LocalMachine\My" | Where-Object { $_.Issuer.Contains("CN=$subjectPrefix") } | Remove-Item
    Get-ChildItem "Cert:\LocalMachine\My" | Where-Object { $_.Issuer.Contains("CN=$groupCertCommonName") } | Remove-Item
    Get-ChildItem "Cert:\LocalMachine\My" | Where-Object { $_.Issuer.Contains("CN=$deviceCertCommonName") } | Remove-Item
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

$storageAccountName = "$($ResourceGroup.ToLower())sa"
$hubName = $ResourceGroup
$appRegistrationName = $ResourceGroup
$deviceProvisioningServiceName = $ResourceGroup
$farRegion = "southeastasia"
$farHubName = $ResourceGroup + "Far"
$uploadCertificateName = "group1-certificate"

########################################################################################################
# Generate self-signed certs and to use in DPS and IoT Hub
# New certs will be generated each time you run the script as the script cleans up in the end
########################################################################################################

$rootCommonName = "$subjectPrefix Test Root CA"
$intermediateCert1CommonName = "$subjectPrefix Intermediate 1 CA"
$intermediateCert2CommonName = "$subjectPrefix Intermediate 2 CA"
$groupCertCommonName = "xdevice1"
$deviceCertCommonName = "iothubx509device1"
$iotHubCertCommonName = "iothubx509device1"

$rootCertPath = "./Root.cer"
$individualDeviceCertPath = "./Device.cer"
$verificationCertPath = "./verification.cer"

$groupPfxPath = "./Group.pfx"
$individualDevicePfxPath = "./Device.pfx"
$iotHubPfxPath = "./IotHub.pfx"

$groupCertChainPath = "./GroupCertChain.p7b"

############################################################################################################################
# Cleanup old certs and files that can cause a conflict
############################################################################################################################
CleanUp-Certs

# Generate self signed Root and Intermediate CA cert, expiring in 2 years
# These certs are used for signing so ensure to have the correct KeyUsage - CertSign and TestExtension - ca=TRUE&pathlength=12

Write-Host "`nGenerating self signed certs"

$rootCACert = New-SelfSignedCertificate `
    -DnsName "$rootCommonName" `
    -KeyUsage CertSign `
    -TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)

$intermediateCert1 = New-SelfSignedCertificate `
    -DnsName "$intermediateCert1CommonName" `
    -KeyUsage CertSign `
    -TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $rootCACert

$intermediateCert2 = New-SelfSignedCertificate `
    -DnsName "$intermediateCert2CommonName" `
    -KeyUsage CertSign `
    -TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $intermediateCert1

# Create Certificate chain from Root to IntermediateCert2. This chain will be combined with the cert signed by IntermediateCert2 to test group enrollment.
# Chain: Root->Intermediate1->Intermediate2, cert: Intermediate2->deviceCert
Get-ChildItem "Cert:\LocalMachine\My" | Where-Object { $_.Issuer.contains("CN=$subjectPrefix") } | Export-Certificate -FilePath $groupCertChainPath -Type p7b | Out-Null

Export-Certificate -cert $rootCACert -FilePath $rootCertPath -Type CERT | Out-Null

$certPassword = ConvertTo-SecureString $GroupCertificatePassword -AsPlainText -Force

# Create leaf certificates, expiring in 2 years
# These certs are not used for signing so don't specify KeyUsage and TestExtension - ca=TRUE&pathlength=12

# Certificate for enrollment of a device using group enrollment.
$groupDeviceCert = New-SelfSignedCertificate `
    -DnsName "$groupCertCommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $intermediateCert2

Export-PFXCertificate -cert $groupDeviceCert -filePath $groupPfxPath -password $certPassword | Out-Null
$DPS_GROUPX509_PFX_CERTIFICATE = [Convert]::ToBase64String((Get-Content $groupPfxPath -Encoding Byte))

# Certificate for enrollment of a device using individual enrollment.
$individualDeviceCert = New-SelfSignedCertificate `
    -DnsName "$deviceCertCommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)

Export-Certificate -cert $individualDeviceCert -FilePath $individualDeviceCertPath -Type CERT | Out-Null
Export-PFXCertificate -cert $individualDeviceCert -filePath $individualDevicePfxPath -password $certPassword | Out-Null
$DPS_INDIVIDUALX509_PFX_CERTIFICATE = [Convert]::ToBase64String((Get-Content $individualDevicePfxPath -Encoding Byte))

# IoT hub certificate for authemtication. The tests are not setup to use a password for the certificate so create the certificate is created with no password.
$iotHubCert = New-SelfSignedCertificate `
    -DnsName "$iotHubCertCommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)

$iotHubCredentials = New-Object System.Management.Automation.PSCredential("Password", (New-Object System.Security.SecureString))
Export-PFXCertificate -cert $iotHubCert -filePath $iotHubPfxPath -password $iotHubCredentials.Password | Out-Null
$IOTHUB_X509_PFX_CERTIFICATE = [Convert]::ToBase64String((Get-Content $iotHubPfxPath -Encoding Byte))

$DPS_GROUPX509_CERTIFICATE_CHAIN = [Convert]::ToBase64String((Get-Content $groupCertChainPath -Encoding Byte))
$DPS_X509_PFX_CERTIFICATE_PASSWORD = $GroupCertificatePassword

########################################################################################################
# Install latest version of az cli
########################################################################################################

if ($InstallDependencies)
{
    Write-Host "`nInstalling and updating AZ CLI"
    Install-Module -Name Az -AllowClobber -Force
    Update-Module -Name Az
}

########################################################################################################
# Install chocolatey and docker
########################################################################################################

if ($InstallDependencies)
{
    Write-Host "`nSetting up docker"
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
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
    Write-Host "`nInstalling azure iot cli extensions"
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
    Write-Host "`nCreating Resource Group $ResourceGroup in $Region"
    az group create --name $ResourceGroup --location $Region --output none
}

#######################################################################################################
# Invoke-Deployment - Uses the .\.json template to
# create the necessary resources to run E2E tests.
#######################################################################################################

# Create a unique deployment name
$randomSuffix = -join ((65..90) + (97..122) | Get-Random -Count 5 | ForEach-Object { [char]$_ })
$deploymentName = "IotE2eInfra-$randomSuffix"

# Deploy
Write-Host @"
    `nStarting deployment which may take a while
    1.Progress can be monitored from the Azure Portal (http://portal.azure.com)
    2.Deployment name ($deploymentName), Resource group ($ResourceGroup), Subscription ($SubscriptionId)
"@

az deployment group create  `
    --resource-group $ResourceGroup `
    --name $deploymentName `
    --template-file "$PSScriptRoot\e2eTestsArmTemplate.json" `
    --output none `
    --parameters `
    Region=$Region `
    ResourceGroup=$ResourceGroup `
    StorageAccountName=$storageAccountName `
    DeviceProvisioningServiceName=$deviceProvisioningServiceName `
    HubName=$hubName `
    FarHubName=$farHubName `
    FarRegion=$farRegion `
    UserObjectId=$userObjectId

Write-Host "`nYour infrastructure is ready in subscription ($SubscriptionId), resource group ($ResourceGroup)"

#########################################################################################################
# Get propreties to setup the config file for Environment variables
#########################################################################################################

Write-Host "`nGetting secrets from ARM template output"
$iotHubThumbprint = "CADB8E398FA9C7DD382E2ED092258BB3D916652C"
$iotHubConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.hubConnectionString.value' --output tsv
$farHubConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.farHubConnectionString.value' --output tsv
$eventHubConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName  --query 'properties.outputs.eventHubConnectionString.value' --output tsv
$storageAccountConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName  --query 'properties.outputs.storageAccountConnectionString.value' --output tsv
$deviceProvisioningServiceConnectionString = az deployment group show -g $ResourceGroup -n $deploymentName  --query 'properties.outputs.deviceProvisioningServiceConnectionString.value' --output tsv
$eventResourceGroup = az resource show -g $ResourceGroup --resource-type microsoft.devices/iothubs -n $ResourceGroup --query 'properties.eventHubEndpoints.events.path' --output tsv
$workspaceId = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.workspaceId.value' --output tsv
$keyVaultName = az deployment group show -g $ResourceGroup -n $deploymentName --query 'properties.outputs.keyVaultName.value' --output tsv
$consumerGroups = "e2e-tests"
$proxyServerAddress = "127.0.0.1:8888"

##################################################################################################################################
# Uploading certificate to DPS, verifying and creating enrollment groups
##################################################################################################################################

$dpsIdScope = az iot dps show -g $ResourceGroup --name $deviceProvisioningServiceName --query 'properties.idScope' --output tsv
$certExits = az iot dps certificate list -g $ResourceGroup --dps-name $deviceProvisioningServiceName --query "value[?name=='$uploadCertificateName']" --output tsv
if ($certExits)
{
    Write-Host "`nDeleting existing certificate from DPS"
    $etag = az iot dps certificate show -g $ResourceGroup --dps-name $deviceProvisioningServiceName --certificate-name $uploadCertificateName --query 'etag'
    az iot dps certificate delete -g $ResourceGroup --dps-name $deviceProvisioningServiceName  --name $uploadCertificateName --etag $etag
}
Write-Host "`nUploading new certificate to DPS"
az iot dps certificate create -g $ResourceGroup --path $rootCertPath --dps-name $deviceProvisioningServiceName --certificate-name $uploadCertificateName --output none

$isVerified = az iot dps certificate show -g $ResourceGroup --dps-name $deviceProvisioningServiceName --certificate-name $uploadCertificateName --query 'properties.isVerified' --output tsv
if ($isVerified -eq 'false')
{
    Write-Host "`nVerifying certificate uploaded to DPS"
    $etag = az iot dps certificate show -g $ResourceGroup --dps-name $deviceProvisioningServiceName --certificate-name $uploadCertificateName --query 'etag'
    $requestedCommonName = az iot dps certificate generate-verification-code -g $ResourceGroup --dps-name $deviceProvisioningServiceName --certificate-name $uploadCertificateName -e $etag --query 'properties.verificationCode'
    $verificationCertArgs = @{
        "-DnsName"                       = $requestedCommonName;
        "-CertStoreLocation"             = "cert:\LocalMachine\My";
        "-NotAfter"                      = (get-date).AddYears(2);
        "-TextExtension"                 = @("2.5.29.37={text}1.3.6.1.5.5.7.3.2,1.3.6.1.5.5.7.3.1", "2.5.29.19={text}ca=FALSE&pathlength=0"); 
        "-Signer"                        = $rootCACert;
    }
    $verificationCert = New-SelfSignedCertificate @verificationCertArgs
    Export-Certificate -cert $verificationCert -filePath $verificationCertPath -Type Cert | Out-Null
    $etag = az iot dps certificate show -g $ResourceGroup --dps-name $deviceProvisioningServiceName --certificate-name $uploadCertificateName --query 'etag'
    az iot dps certificate verify -g $ResourceGroup --dps-name $deviceProvisioningServiceName --certificate-name $uploadCertificateName -e $etag --path $verificationCertPath --output none
}

$groupEnrollmentId = "Group1"
$groupEnrollmentExists = az iot dps enrollment-group list -g $ResourceGroup  --dps-name $deviceProvisioningServiceName --query "[?enrollmentGroupId=='$groupEnrollmentId'].enrollmentGroupId" --output tsv
if ($groupEnrollmentExists)
{
    Write-Host "`nDeleting existing group enrollment $groupEnrollmentId"
    az iot dps enrollment-group delete -g $ResourceGroup --dps-name $deviceProvisioningServiceName --enrollment-id $groupEnrollmentId
}
Write-Host "`nAdding group enrollment $groupEnrollmentId"
az iot dps enrollment-group create -g $ResourceGroup --dps-name $deviceProvisioningServiceName --enrollment-id $groupEnrollmentId --ca-name $uploadCertificateName --output none

$individualEnrollmentId = "iothubx509device1"
$individualDeviceId = "provisionedx509device1"
$individualEnrollmentExists = az iot dps enrollment list -g $ResourceGroup  --dps-name $deviceProvisioningServiceName --query "[?deviceId=='$individualDeviceId'].deviceId" --output tsv
if ($individualEnrollmentExists)
{
    Write-Host "`nDeleting existing individual enrollment $individualEnrollmentId for device $individualDeviceId"
    az iot dps enrollment delete -g $ResourceGroup --dps-name $deviceProvisioningServiceName --enrollment-id $individualEnrollmentId
}
Write-Host "`nAdding individual enrollment $individualEnrollmentId for device $individualDeviceId"
az iot dps enrollment create `
    -g $ResourceGroup `
    --dps-name $deviceProvisioningServiceName `
    --enrollment-id $individualEnrollmentId `
    --device-id $individualDeviceId `
    --attestation-type x509 `
    --certificate-path $individualDeviceCertPath `
    --output none

#################################################################################################################################################
# Configure an AAD app and create self signed certs and get the bytes to generate more content info.
#################################################################################################################################################

$appId = az ad app list --show-mine --query "[?displayName=='$appRegistrationName'].appId" --output tsv
if (-not $appId)
{
    Write-Host "`nCreating App Registration $appRegistrationName"
    $appId = az ad app create --display-name $appRegistrationName --reply-urls https://api.loganalytics.io/ --available-to-other-tenants false --query 'appId' --output tsv --output none
    Write-Host "`nApplication $appRegistrationName with Id $appId was created successfully."
}
$appId = az ad app list --show-mine --query "[?displayName=='$appRegistrationName'].appId" --output tsv

$spExists = az ad sp list --show-mine --query "[?appId=='$appId'].appId" --output tsv
if (-not $spExists)
{
    Write-Host "`nCreating the service principal for the app registration if it does not exist"
    az ad sp create --id $appId --output none
}

# The Service Principal takes a while to get propogated and if a different endpoint is hit before that, trying to grant a permission will fail.
# Adding retries so that we can grant the permissions successfully without re-running the script.
Write-Host "`nGranting $appId Reader role assignment to the $Resourcegroup resource group."
$tries = 0;
while (++$tries -le 10)
{
    try
    {
        az role assignment create --role Reader --assignee $appId --resource-group $ResourceGroup --output none
        break
    }
    catch
    {
        if ($tries -ge 10)
        {
            Write-Error "Max retries reached for granting service principal permissions."
            throw
        }

        Write-Host "Granting service prinpcal permission failed. Waiting 5 seconds before retry..."
        Start-Sleep -s 5;
    }
}

Write-Host "`nCreating a self-signed certificate and placing it in $ResourceGroup"
az ad app credential reset --id $appId --create-cert --keyvault $keyVaultName --cert $ResourceGroup --output none
Write-Host "`nSuccessfully created a self signed certificate for your application $appRegistrationName in $ResourceGroup key vault with cert name: $ResourceGroup";

Write-Host "`nFetching the certificate binary"
$selfSignedCerts = "$PSScriptRoot\selfSignedCerts"
if (Test-Path $selfSignedCerts -PathType Leaf)
{
    Remove-Item -r $selfSignedCerts
}

az keyvault secret download --file $selfSignedCerts --vault-name $keyVaultName -n $ResourceGroup --encoding base64
$fileContent = Get-Content $selfSignedCerts -Encoding Byte
$fileContentB64String = [System.Convert]::ToBase64String($fileContent);

Write-Host "`nSuccessfully fetched the certificate bytes ... removing the cert file from the disk"
Remove-Item -r $selfSignedCerts

###################################################################################################################################
# Store all secrets in a KeyVault - Values will be pulled down from here to configure environment variables
###################################################################################################################################

Write-Host("`nWriting secrets to KeyVault $keyVaultName")
az keyvault set-policy -g $ResourceGroup --name $keyVaultName --object-id $userObjectId --secret-permissions delete get list set --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-CONN-STRING-CSHARP" --value $iotHubConnectionString --output none
# Iot Hub Connection string Environment variable for Java
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB_CONNECTION_STRING" --value $iotHubConnectionString --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-PFX-X509-THUMBPRINT" --value $iotHubThumbprint --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-EVENTHUB-CONN-STRING-CSHARP" --value $eventHubConnectionString --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-EVENTHUB-COMPATIBLE-NAME" --value $eventResourceGroup --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-EVENTHUB-CONSUMER-GROUP" --value $consumerGroups --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-PROXY-SERVER-ADDRESS" --value $proxyServerAddress --output none
az keyvault secret set --vault-name $keyVaultName --name "FAR-AWAY-IOTHUB-HOSTNAME" --value "$farHubName.azure-devices.net" --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-IDSCOPE" --value $dpsIdScope --output none
# DPS ID Scope Environment variable for Java
az keyvault secret set --vault-name $keyVaultName --name "IOT_DPS_ID_SCOPE" --value $dpsIdScope --output none
az keyvault secret set --vault-name $keyVaultName --name "PROVISIONING-CONNECTION-STRING" --value $deviceProvisioningServiceConnectionString --output none
# DPS Connection string Environment variable for Java
az keyvault secret set --vault-name $keyVaultName --name "IOT_DPS_CONNECTION_STRING" --value $deviceProvisioningServiceConnectionString --output none
az keyvault secret set --vault-name $keyVaultName --name "CUSTOM-ALLOCATION-POLICY-WEBHOOK" --value $CustomAllocationPolicyWebHook --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-GLOBALDEVICEENDPOINT" --value "global.azure-devices-provisioning.net" --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-X509-PFX-CERTIFICATE-PASSWORD" --value $DPS_X509_PFX_CERTIFICATE_PASSWORD --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-X509-PFX-CERTIFICATE" --value $IOTHUB_X509_PFX_CERTIFICATE --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-INDIVIDUALX509-PFX-CERTIFICATE" --value $DPS_INDIVIDUALX509_PFX_CERTIFICATE --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-GROUPX509-PFX-CERTIFICATE" --value $DPS_GROUPX509_PFX_CERTIFICATE --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-GROUPX509-CERTIFICATE-CHAIN" --value $DPS_GROUPX509_CERTIFICATE_CHAIN --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-DEVICE-CONN-STRING-INVALIDCERT" --value "HostName=invalidcertiothub1.westus.cloudapp.azure.com;DeviceId=DoNotDelete1;SharedAccessKey=zWmeTGWmjcgDG1dpuSCVjc5ZY4TqVnKso5+g1wt/K3E=" --output none
az keyvault secret set --vault-name $keyVaultName --name "IOTHUB-CONN-STRING-INVALIDCERT" --value "HostName=invalidcertiothub1.westus.cloudapp.azure.com;SharedAccessKeyName=iothubowner;SharedAccessKey=Fk1H0asPeeAwlRkUMTybJasksTYTd13cgI7SsteB05U=" --output none
az keyvault secret set --vault-name $keyVaultName --name "PROVISIONING-CONNECTION-STRING-INVALIDCERT" --value "HostName=invalidcertdps1.westus.cloudapp.azure.com;SharedAccessKeyName=provisioningserviceowner;SharedAccessKey=lGO7OlXNhXlFyYV1rh9F/lUCQC1Owuh5f/1P0I1AFSY=" --output none
az keyvault secret set --vault-name $keyVaultName --name "STORAGE-ACCOUNT-CONNECTION-STRING" --value $storageAccountConnectionString --output none
az keyvault secret set --vault-name $keyVaultName --name "LA-WORKSPACE-ID" --value $workspaceId --output none
az keyvault secret set --vault-name $keyVaultName --name "LA-AAD-TENANT" --value "72f988bf-86f1-41af-91ab-2d7cd011db47" --output none
az keyvault secret set --vault-name $keyVaultName --name "LA-AAD-APP-ID" --value $appId --output none
az keyvault secret set --vault-name $keyVaultName --name "LA-AAD-APP-CERT-BASE64" --value $fileContentB64String --output none
az keyvault secret set --vault-name $keyVaultName --name "DPS-GLOBALDEVICEENDPOINT-INVALIDCERT" --value "invalidcertgde1.westus.cloudapp.azure.com" --output none
# Below Environment variables are only used in Java
az keyvault secret set --vault-name $keyVaultName --name "FAR_AWAY_IOTHUB_CONNECTION_STRING" --value $farHubConnectionString--output none
az keyvault secret set --vault-name $keyVaultName --name "IS_BASIC_TIER_HUB" --value "false" --output none
###################################################################################################################################
# Run docker containers for TPM simulators and Proxy
###################################################################################################################################

if (-not (docker images -q aziotbld/testtpm))
{
    Write-Host "Setting up docker container for TPM simulator"
    docker run -d --restart unless-stopped --name azure-iot-tpmsim -p 127.0.0.1:2321:2321 -p 127.0.0.1:2322:2322 aziotbld/testtpm
}

if (-not (docker images -q aziotbld/testproxy))
{
    Write-Host "Setting up docker container for Proxy"
    docker run -d --restart unless-stopped --name azure-iot-tinyproxy -p 127.0.0.1:8888:8888 aziotbld/testproxy
}

############################################################################################################################
# Clean up certs and files created by the script
############################################################################################################################
CleanUp-Certs

# Creating a file to run to load environment variables
$loadScriptDir = Join-Path $PSScriptRoot "..\..\..\..\.." -Resolve
$loadScriptName = "Load-$keyVaultName.ps1"
Write-Host "`nWriting environment loading file to $loadScriptDir\$loadScriptName"
$file = New-Item -Path $loadScriptDir -Name $loadScriptName -ItemType "file" -Force
Add-Content -Path $file.PSPath -Value "$PSScriptRoot\LoadEnvironmentVariablesFromKeyVault.ps1 -SubscriptionId $SubscriptionId -KeyVaultName $keyVaultName"

############################################################################################################################
# Configure Environment Variables
############################################################################################################################
Invoke-Expression "$loadScriptDir\$loadScriptName"

$endTime = (Get-Date)
$ElapsedTime = (($endTime - $startTime).TotalMinutes)
Write-Host "`n`nCompleted in $ElapsedTime minutes. Run the generated file $loadScriptDir\$loadScriptName to load environment variables for future runs. Values will be overwritten whenever you run e2eTestsSetup.ps1 with a same resource group name.`n"
