$certificateHashAlgorithm = "SHA256"
$groupCertificatePassword = "password"

$subjectPrefix = "IoT Test";
$rootCommonName = "$subjectPrefix Root CA";
$intermediateCert1CommonName = "$subjectPrefix Intermediate 1 CA";
$intermediateCert2CommonName = "$subjectPrefix Intermediate 2 CA";
$leafDeviceCertCommonName = "iot-test-leaf-device-certificate";
$leafDeviceCert2CommonName = "iot-test-leaf-device-certificate2";
$leafDeviceCert3CommonName = "iot-test-leaf-device-certificate3";

$scriptRoot = "H:/Workspace/Code/CSharp/DPS_X509"
$rootPfxPath = Join-Path $scriptRoot "root.pfx";
$intermediateCert1PfxPath = Join-Path $scriptRoot "intermediateCert1.pfx";
$intermediateCert2PfxPath = Join-Path $scriptRoot "intermediateCert2.pfx"
$leafDeviceCertPfxPath = Join-Path $scriptRoot "leaf-device.pfx";
$leafDeviceCert2PfxPath = Join-Path $scriptRoot "leaf-device2.pfx";
$leafDeviceCert3PfxPath = Join-Path $scriptRoot "leaf-device3.pfx";
$verificationCertPath = Join-Path $scriptRoot "verification.cer";

# Generate self signed Root and Intermediate CA cert, expiring in 2 years
# These certs are used for signing so ensure to have the correct KeyUsage - CertSign and TestExtension - ca=TRUE&pathlength=12

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

$leafDeviceCert = New-SelfSignedCertificate `
    -DnsName "$leafDeviceCertCommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -HashAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $intermediateCert2

$leafDevice2Cert = New-SelfSignedCertificate `
    -DnsName "$leafDeviceCert2CommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -HashAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $intermediateCert2

$leafDevice3Cert = New-SelfSignedCertificate `
    -DnsName "$leafDeviceCert3CommonName" `
    -KeySpec Signature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2") `
    -HashAlgorithm "$certificateHashAlgorithm" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Signer $intermediateCert2

$certPassword = ConvertTo-SecureString $groupCertificatePassword -AsPlainText -Force

# Export the root certificate as a pfx file.
Export-PFXCertificate -cert $rootCACert -filePath $rootPfxPath -password $certPassword | Out-Null

# Export the intermediate1 certificate as a pfx file.
Export-PFXCertificate -cert $intermediateCert1 -filePath $intermediateCert1PfxPath -password $certPassword | Out-Null

# Export the intermediate2 certificate as a pfx file. This certificate will be used to sign and generate the device certificates that are used in DPS group enrollment E2E tests.
Export-PFXCertificate -cert $intermediateCert2 -filePath $intermediateCert2PfxPath -password $certPassword | Out-Null

# Export the leaf device certificates as pfx file.
Export-PFXCertificate -cert $leafDeviceCert -filePath $leafDeviceCertPfxPath -password $certPassword | Out-Null
Export-PFXCertificate -cert $leafDevice2Cert -filePath $leafDeviceCert2PfxPath -password $certPassword | Out-Null
Export-PFXCertificate -cert $leafDevice3Cert -filePath $leafDeviceCert3PfxPath -password $certPassword | Out-Null

# Export the base64 encoded certificates from certificate manager

# Verify root CA on DPS
$dpsUploadCertificateName = "root-certificate"
$requestedCommonName = "701734EDAC07BBB5AAF0CC337FA3A481D525E24251D91127"
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

##################################################################################################################################
# Creating enrollment groups.
##################################################################################################################################

$groupEnrollmentId = "group1-root-verified"
Write-Host "`nAdding group enrollment $groupEnrollmentId."
az iot dps enrollment-group create -g $ResourceGroup --dps-name $dpsName --enrollment-id $groupEnrollmentId --ca-name $dpsUploadCertificateName --output none