# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Setup parameters
$subjectPrefix = "IoT Test"
$rootCommonName = "$subjectPrefix Test Root CA"
$intermediateCert1CommonName = "$subjectPrefix Intermediate 1 CA"
$intermediateCert2CommonName = "$subjectPrefix Intermediate 2 CA"
$iotHubCertChainDeviceCommonName = "iothubx509chaindevice1"
$certFolder = "<Path on your machine to create the certs>"
$iotHubCredentials = New-Object System.Management.Automation.PSCredential("Password", (New-Object System.Security.SecureString))
$resourceGroup = "<Resource group name of your hub>"
$hubName = "<Your IotHub name>"
$certNameToUpload = "rootCA"

# Create root cert
$rootCertPath = "$certFolder/root.cer"
$rootCACert = New-SelfSignedCertificate `
    -DnsName "$rootCommonName" `
    -KeyUsage CertSign `
    -TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)
Export-Certificate -cert $rootCACert -FilePath $rootCertPath -Type CERT | Out-Null

# Create intermediate 1 cert
$intermediate1CertPath = "$certFolder/intermediate1.cer"
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
$deviceCert = New-SelfSignedCertificate `
    -DnsName "$iotHubCertChainDeviceCommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $intermediateCert2
Export-PFXCertificate -cert $deviceCert -filePath $devicePfxPath -password $iotHubCredentials.Password | Out-Null

# Upload rootCA certificate to IotHub
$certExits = az iot hub certificate list -g $resourceGroup --hub-name $hubName --query "value[?name=='$certNameToUpload']" --output tsv
if ($certExits)
{
    $etag = az iot hub certificate show -g $resourceGroup --hub-name $hubName --name $certNameToUpload --query 'etag'
    az iot hub certificate delete -g $resourceGroup --hub-name $hubName --name $certNameToUpload --etag $etag
}
az iot hub certificate create -g $resourceGroup --hub-name $hubName --name $certNameToUpload --path $rootCertPath

# Verify rootCA cert in IotHub
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