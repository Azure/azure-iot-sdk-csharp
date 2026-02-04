# Run the DPS Certificate Issuance Sample (C#)
# This script sets up all required environment variables and runs the sample

param(
    [string]$RegistrationId = "test-device-01",
    [string]$TransportType = "Mqtt"
)

# DPS Configuration from your provisioned resources
$provisioningHost = "global.azure-devices-provisioning.net"
$idScope = "0ne0112785F"

# Enrollment group primary key (from your enrollment: 3-enrollment)
$enrollmentPrimaryKey = "Rx9Zt5b6zJwa1cnXizLbYVUSbVBzK5Sg6qXvoGU1G/ZHD/I7kdGcTWoV3jXOEaUYMw5u0bBYopXiAIoTkJfkGg=="

# File paths for CSR key and issued certificate
$csrKeyFile = "$PSScriptRoot\$RegistrationId-csr-key.pem"
$issuedCertFile = "$PSScriptRoot\$RegistrationId-issued-cert.pem"

Write-Host "=========================================="
Write-Host "DPS Certificate Issuance Sample (C#)"
Write-Host "=========================================="
Write-Host "Registration ID: $RegistrationId"
Write-Host "DPS Host: $provisioningHost"
Write-Host "ID Scope: $idScope"
Write-Host "Transport: $TransportType"
Write-Host "=========================================="

# Derive symmetric key from enrollment group key
Write-Host "`nDeriving symmetric key for device..."
$hmac = New-Object System.Security.Cryptography.HMACSHA256
$hmac.Key = [Convert]::FromBase64String($enrollmentPrimaryKey)
$derivedKey = [Convert]::ToBase64String($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($RegistrationId)))

Write-Host "Derived SAS Key: $($derivedKey.Substring(0, 20))..."
Write-Host "=========================================="

Write-Host "`nOutput files will be:"
Write-Host "  Private Key: $csrKeyFile"
Write-Host "  Issued Cert: $issuedCertFile"
Write-Host "=========================================="

# Run the C# sample
Write-Host "`nRunning C# CSR Provisioning Sample...`n"

$samplePath = $PSScriptRoot

dotnet run --project "$samplePath\CsrProvisioningSample.csproj" -- `
    --IdScope "$idScope" `
    --RegistrationId "$RegistrationId" `
    --GlobalDeviceEndpoint "$provisioningHost" `
    --SymmetricKey "$derivedKey" `
    --AuthType SymmetricKey `
    --TransportType "$TransportType" `
    --CsrKeyType ECC `
    --OutputCertPath "$issuedCertFile" `
    --OutputKeyPath "$csrKeyFile" `
    --SendTelemetry true

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n=========================================="
    Write-Host "Sample completed successfully!"
    Write-Host "=========================================="
    Write-Host "Generated files:"
    if (Test-Path $csrKeyFile) {
        Write-Host "  Private Key: $csrKeyFile"
    }
    if (Test-Path $issuedCertFile) {
        Write-Host "  Issued Cert: $issuedCertFile"
    }
} else {
    Write-Host "`n=========================================="
    Write-Host "Sample failed with exit code: $LASTEXITCODE"
    Write-Host "=========================================="
}
