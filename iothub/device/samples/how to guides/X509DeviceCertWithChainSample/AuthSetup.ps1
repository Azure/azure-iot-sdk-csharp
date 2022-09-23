# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param(
    # Local path where the generated certificates will be stored.
    [Parameter(Mandatory=$true)]
    [string] $certFolderPath,

    # The password to use for the root certificate.
    [Parameter(Mandatory=$true)]
    [securestring] $rootCertPassword,

    # Resource group name of the IoT hub.
    [Parameter(Mandatory=$true)]
    [string] $iotHubResourceGroup,

    # IoT hub name used for running the sample.
    [Parameter(Mandatory=$true)]
    [string] $iotHubName,

    # Device Id created to use in sample.
    [Parameter(Mandatory=$true)]
    [string] $deviceId
)

# Check that script is run in admin mode
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin)
{
    throw "This script must be run in administrative mode."
}

# Setup parameters
$subjectPrefix = "IoT Test"
$rootCommonName = "$subjectPrefix Root CA"
$intermediateCert1CommonName = "$subjectPrefix Intermediate 1 CA"
$intermediateCert2CommonName = "$subjectPrefix Intermediate 2 CA"
$iotHubCertChainDeviceCommonName = $deviceId
$certFolder = $certFolderPath
$iotHubCredentials = New-Object System.Management.Automation.PSCredential("Password", $rootCertPassword)
$resourceGroup = $iotHubResourceGroup
$hubName = $iotHubName
$certNameToUpload = "rootCA"

# Create root cert
$rootCertPath = "$certFolder/root.cer"
Write-Host "Creating $rootCertPath rootCACert"
$rootCACert = New-SelfSignedCertificate `
    -DnsName "$rootCommonName" `
    -KeyUsage CertSign `
    -TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)
Export-Certificate -cert $rootCACert -FilePath $rootCertPath -Type CERT | Out-Null

# Create intermediate 1 cert
$intermediate1CertPath = "$certFolder/intermediate1.cer"
Write-Host "Creating $intermediate1CertPath signed by rootCACert"
$intermediateCert1 = New-SelfSignedCertificate `
    -DnsName "$intermediateCert1CommonName" `
    -KeyUsage CertSign `
    -TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $rootCACert
Export-Certificate -cert $intermediateCert1 -FilePath $intermediate1CertPath -Type CERT | Out-Null

# Create intermediate 2 cert
$intermediate2CertPath = "$certFolder/intermediate2.cer"
Write-Host "Creating $intermediate2CertPath signed by intermediateCert1"
$intermediateCert2 = New-SelfSignedCertificate `
    -DnsName "$intermediateCert2CommonName" `
    -KeyUsage CertSign `
    -TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $intermediateCert1
Export-Certificate -cert $intermediateCert2 -FilePath $intermediate2CertPath -Type CERT | Out-Null

# Create device cert signed by intermediate2 Certificate
$devicePfxPath = "$certFolder/$iotHubCertChainDeviceCommonName.pfx"
Write-Host "Creating $devicePfxPath signed by intermediateCert2"
$deviceCert = New-SelfSignedCertificate `
    -DnsName "$iotHubCertChainDeviceCommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $intermediateCert2
Write-Host "Exporting $iotHubCertChainDeviceCommonName.pfx to $devicePfxPath"
Export-PFXCertificate -cert $deviceCert -filePath $devicePfxPath -password $iotHubCredentials.Password | Out-Null

# Upload rootCA certificate to IotHub
Write-Host "Uploading $rootCertPath to $hubName"
$certExits = az iot hub certificate list -g $resourceGroup --hub-name $hubName --query "value[?name=='$certNameToUpload']" --output tsv
if ($certExits)
{
    $etag = az iot hub certificate show -g $resourceGroup --hub-name $hubName --name $certNameToUpload --query 'etag'
    az iot hub certificate delete -g $resourceGroup --hub-name $hubName --name $certNameToUpload --etag $etag
}
az iot hub certificate create -g $resourceGroup --hub-name $hubName --name $certNameToUpload --path $rootCertPath | Out-Null

# Verify rootCA cert in IotHub
Write-Host "Verifying possession of rootCACert in $hubName"
$etag = az iot hub certificate show -g $resourceGroup --hub-name $hubName --name $certNameToUpload --query 'etag'
$requestedCommonName = az iot hub certificate generate-verification-code -g $resourceGroup --hub-name $hubName --name $certNameToUpload -e $etag --query 'properties.verificationCode'
$verificationCertArgs = @{
    "-DnsName"                       = $requestedCommonName;
    "-CertStoreLocation"             = "cert:\LocalMachine\My";
    "-NotAfter"                      = (get-date).AddYears(2);
    "-TextExtension"                 = @("2.5.29.37={text}1.3.6.1.5.5.7.3.2,1.3.6.1.5.5.7.3.1", "2.5.29.19={text}ca=FALSE&pathlength=0"); 
    "-Signer"                        = $rootCACert;
}
$verificationCertPath = "$certFolder/verification.cer"
$verificationCert = New-SelfSignedCertificate @verificationCertArgs
Export-Certificate -cert $verificationCert -filePath $verificationCertPath -Type Cert | Out-Null
$etag = az iot hub certificate show -g $resourceGroup --hub-name $hubName --name $certNameToUpload --query 'etag'
az iot hub certificate verify -g $resourceGroup --hub-name $hubName --name $certNameToUpload -e $etag --path $verificationCertPath --output none
Write-Host "Successfully verified possession of rootCACert in $hubName"