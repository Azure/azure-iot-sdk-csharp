# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param(
    # Local path where the generated certificates will be stored.
    [Parameter(Mandatory=$true)]
    [string] $certFolderPath,

    # The password to use for the root certificate.
    [Parameter(Mandatory=$true)]
    [securestring] $rootCertPassword,

    # Resource group name of the IoT dps.
    [Parameter(Mandatory=$true)]
    [string] $dpsResourceGroup,

    # IoT dps name used for running the sample.
    [Parameter(Mandatory=$true)]
    [string] $dpsName,

    # Device Id created to use in sample.
    [Parameter(Mandatory=$true)]
    [string] $deviceId,

    # Delete and recreate enrollmentGroup if it already exists
    [Parameter(Mandatory=$false)]
    [switch] $force = $false
)

Write-Host $force
# Check that script is run in admin mode as it requires admin permission to create self-signed certificates. 
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin)
{
    throw "This script must be run in administrative mode for creating self-signed certificates."
}

# Setup parameters
$subjectPrefix = "IoT Test"
$rootCommonName = "$subjectPrefix Root CA"
$intermediateCertCommonName = "$subjectPrefix Intermediate CA"
$dpsCertChainDeviceCommonName = $deviceId
$certFolder = $certFolderPath
$dpsCredentials = New-Object System.Management.Automation.PSCredential("Password", $rootCertPassword)
$resourceGroup = $dpsResourceGroup
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

# Create intermediate cert
$intermediateCertPath = "$certFolder/intermediate.cer"
Write-Host "Creating $intermediateCertPath signed by rootCACert"
$intermediateCert = New-SelfSignedCertificate `
    -DnsName "$intermediateCertCommonName" `
    -KeyUsage CertSign `
    -TextExtension @("2.5.29.19={text}ca=TRUE&pathlength=12") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $rootCACert
Export-Certificate -cert $intermediateCert -FilePath $intermediateCertPath -Type CERT | Out-Null

# Create device cert signed by intermediate Certificate
$devicePfxPath = "$certFolder/$dpsCertChainDeviceCommonName.pfx"
Write-Host "Creating $devicePfxPath signed by intermediateCert"
$deviceCert = New-SelfSignedCertificate `
    -DnsName "$dpsCertChainDeviceCommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $intermediateCert
Write-Host "Exporting $dpsCertChainDeviceCommonName.pfx to $devicePfxPath"

Export-PFXCertificate -cert $deviceCert -filePath $devicePfxPath -password $dpsCredentials.Password | Out-Null

# Check if the resource group exists. If not, exit.
$resourceGroupExists = az group exists -n $resourceGroup
if ($resourceGroupExists -ne $true)
{
    Write-Host "Resource Group '$resourceGroup' does not exist. Exiting..."
    exit
}

# Check if the DPS instance exists. If not, exit.
$dpsExists = az iot dps show --name $dpsName -g $resourceGroup
if ($dpsExists -eq $null)
{
    Write-Host "DPS instance '$dpsName' does not exist under '$resourceGroup'. Exiting..."
    exit
}

# Upload rootCA certificate to DPS
Write-Host "Uploading $rootCertPath to $dpsName"
$certExits = az iot dps certificate list -g $resourceGroup --dps-name $dpsName --query "value[?name=='$certNameToUpload']" --output tsv
if ($certExits)
{
    $etag = az iot dps certificate show -g $resourceGroup --dps-name $dpsName --certificate-name $certNameToUpload --query 'etag'
    az iot dps certificate delete -g $resourceGroup --dps-name $dpsName --certificate-name $certNameToUpload --etag $etag
}
az iot dps certificate create -g $resourceGroup --dps-name $dpsName --certificate-name $certNameToUpload --path $rootCertPath | Out-Null

$verificationCertPath = "$certFolder/verification.cer"

# Verify rootCA cert in DPS
Write-Host "Verifying possession of rootCACert in $dpsName"
$isVerified = az iot dps certificate show -g $ResourceGroup --dps-name $dpsName --certificate-name $certNameToUpload --query 'properties.isVerified' --output tsv
if ($isVerified -eq 'false')
{
    Write-Host "Verifying certificate uploaded to DPS."
    $etag = az iot dps certificate show -g $ResourceGroup --dps-name $dpsName --certificate-name $certNameToUpload --query 'etag'
    $requestedCommonName = az iot dps certificate generate-verification-code -g $ResourceGroup --dps-name $dpsName --certificate-name $certNameToUpload -e $etag --query 'properties.verificationCode'
    $verificationCertArgs = @{
        "-DnsName"             = $requestedCommonName;
        "-CertStoreLocation"   = "cert:\LocalMachine\My";
        "-NotAfter"            = (get-date).AddYears(2);
        "-TextExtension"       = @("2.5.29.37={text}1.3.6.1.5.5.7.3.2,1.3.6.1.5.5.7.3.1", "2.5.29.19={text}ca=FALSE&pathlength=0");
        "-Signer"              = $rootCACert;
    }
    $verificationCert = New-SelfSignedCertificate @verificationCertArgs
    Export-Certificate -cert $verificationCert -filePath $verificationCertPath -Type Cert | Out-Null
    $etag = az iot dps certificate show -g $ResourceGroup --dps-name $dpsName --certificate-name $certNameToUpload --query 'etag'
    az iot dps certificate verify -g $ResourceGroup --dps-name $dpsName --certificate-name $certNameToUpload -e $etag --path $verificationCertPath --output none
}

$groupEnrollmentId = "x509GroupEnrollment"

# Check certificate file extension.
if (($intermediateCertPath.EndsWith('.pem') -ne $true) -and ($intermediateCertPath.EndsWith('.cer') -ne $true))
{
    Write-Host "Certificate file type must be either '.pem' or '.cer'"
    exit
}

# Check if the enrollment group already exists in dps instance. If it does, delete and regenerate the group enrollment.
Write-Host "`Checking if '$groupEnrollmentId' enrollment group already exists in '$dpsName'..."
$groupEnrollmentExists = az iot dps enrollment-group show --dps-name $dpsName -g $resourceGroup --enrollment-id $groupEnrollmentId
if ($groupEnrollmentExists)
{
    if ($force)
    {
        Write-Host "Deleting existing enrollment group '$groupEnrollmentId' in '$dpsName'..."
        az iot dps enrollment-group delete -g $resourceGroup --eid $groupEnrollmentId --dps-name $dpsName
        Write-Host "Enrollment group '$groupEnrollmentId' is deleted in '$dpsName'."
    }
    else
    {
        Write-Host "Enrollment group '$groupEnrollmentId' already exists under '$dpsName'. If you wish to delete the enrollment group '$groupEnrollmentId', add -force. Exiting..."
        exit
    }
}
else
{
    Write-Host "$groupEnrollmentId enrollment group does not exist in $dpsName"
}

Write-Host "Creating enrollment group '$groupEnrollmentId' in '$dpsName'..."
az iot dps enrollment-group create -g $resourceGroup --dps-name $dpsName --enrollment-id $groupEnrollmentId --certificate-path $intermediateCertPath
Write-Host "Enrollment group '$groupEnrollmentId' is created in '$dpsName'."