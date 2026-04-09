[TimeSpan]$DefaultCertificateExpiration = [TimeSpan]::FromDays(365)

# Generic helper functions
function Debug-PSScript {
    param($Path)

    $Path = Resolve-Path $Path

    $tokens = $null
    $errors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($Path, [ref]$tokens, [ref]$errors) | Out-Null

    $errors | ForEach-Object {
        [pscustomobject]@{
            Message = $_.Message
            File    = $_.Extent.File
            Line    = $_.Extent.StartLineNumber
            Column  = $_.Extent.StartColumnNumber
            Text    = $_.Extent.Text
        }
    } | Format-List
}

function New-Guid {
    param([switch]$NoDashes)

    $Guid = [guid]::NewGuid().ToString()

    if ($NoDashes) {
        $Guid = $Guid.Replace('-', '')
    }

    return $Guid
}

function New-TempFile {
    $TempFilePath = [System.IO.Path]::GetTempFileName()
    Remove-Item -Path $TempFilePath
    return $TempFilePath
}

function ConvertTo-Base64 {
    param($Content)
    $ContentBytes  = [System.Text.Encoding]::UTF8.GetBytes($Content)
    $Base64Content = [System.Convert]::ToBase64String($ContentBytes)
    return $Base64Content
}

function Set-FileContent {
    param(
        $Path = $null,
        $Content = $null
    )

    $OutFileDir = Split-Path -Path $Path -Parent
    if ($OutFileDir -ne "" -and $(Test-Path $OutFileDir) -eq $false) {
        New-Item -ItemType Directory -Force -Path $OutFileDir | Out-Null        
    }

    if ($PSVersionTable.PSVersion.Major -lt 7) {
        $Utf8NoBom = New-Object System.Text.UTF8Encoding($false)  # $false = no BOM
        [System.IO.File]::WriteAllText("$Path", "$Content", $Utf8NoBom)
    } else {
        Set-Content -Path "$Path" -Value $Content -Encoding utf8 -NoNewline
    }
}

function Stop-OnError {
    param([string]$Step = 'Command', [int]$ExpectedReturn = 0, [switch]$Throw)
    if ($LASTEXITCODE -ne $ExpectedReturn) {
        $ErrorMessage = "ERROR: `"$Step`" failed (exit code $LASTEXITCODE)"
        if ($Throw) {
            throw $ErrorMessage
        } else {
            Write-Host $ErrorMessage 
            exit 1
        }
    }
}

function Join-Hashtable {
    param(
        [Hashtable]$Hashtable,
        [string]$Separator = " "
    )

    if ($null -eq $Hashtable -or $Hashtable.Count -eq 0) {
        return ""
    } else {
        return ($Hashtable.GetEnumerator() | %{ "$($_.Key)=$($_.Value)" }) -join $Separator
    }
}

# Certificates

function Export-Pkcs8PrivateKeyPem {
    param($Key) 
    # Key of type [System.Security.Cryptography.RSA] or [System.Security.Cryptography.ECDsa]

    if ($PSVersionTable.PSVersion.Major -lt 7) {
        $KeyPemHex = [Convert]::ToBase64String($Key.Key.Export([System.Security.Cryptography.CngKeyBlobFormat]::Pkcs8PrivateBlob), 'InsertLineBreaks')
        return "-----BEGIN PRIVATE KEY-----`n$KeyPemHex`n-----END PRIVATE KEY-----"
    } else {
        return $Key.ExportPkcs8PrivateKeyPem()
    }
}

function New-RsaPrivateKey {
    param(
        [string]$Path = $null,
        [switch]$Verbose
    )

    $rsa = [System.Security.Cryptography.RSA]::Create(4096)

    if ($null -ne $Path -and $Path -ne "") {
        $pem = Export-Pkcs8PrivateKeyPem -Key $rsa

        if ($Verbose) {
            Write-Host "Saving private key to $Path"
        }

        Set-FileContent -Path $Path -Content $pem
    }

    return $rsa
}

function New-EcdsaPrivateKey {
    param(
        [string]$Curve = "nistP256",
        [string]$Path = $null,
        [switch]$Verbose
    )

    $ecdsa = [System.Security.Cryptography.ECDsa]::Create([System.Security.Cryptography.ECCurve]::CreateFromFriendlyName($Curve))

    if ($null -ne $Path -and $Path -ne "") {
        $pem = Export-Pkcs8PrivateKeyPem -Key $ecdsa

        if ($Verbose) {
            Write-Host "Saving ECDSA private key ($Curve) to $Path"
        }

        Set-FileContent -Path $Path -Content $pem
    }

    return $ecdsa
}

function New-X509CertificateSigningRequest {
    param(
        [string]$Subject,
        $Key = $null,
        [System.Security.Cryptography.HashAlgorithmName]$HashAlgorithm = [System.Security.Cryptography.HashAlgorithmName]::SHA256,
        [Switch]$AsBytes,
        [Switch]$NoHeaders
    )

    $DistinguishedName = [System.Security.Cryptography.X509Certificates.X500DistinguishedName]::new("CN=$Subject")
    $csr = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new($DistinguishedName, $Key, $HashAlgorithm)
    
    if ($AsBytes) {
        return $csr.CreateSigningRequest()
    } else {

        if ($NoHeaders) {
            return [Convert]::ToBase64String($csr.CreateSigningRequest())
        } else {
            $Base64Csr = [Convert]::ToBase64String($csr.CreateSigningRequest(), 'InsertLineBreaks')
            return "-----BEGIN CERTIFICATE REQUEST-----`n$Base64Csr`n-----END CERTIFICATE REQUEST-----"
        }
    }
}

function New-RandomNumber {
    param($Length = 16)

    if ($PSVersionTable.PSVersion.Major -lt 7) {
        $RandomNumber = New-Object byte[] $Length
        [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($RandomNumber)
        return $RandomNumber
    } else {
        return [System.Security.Cryptography.RandomNumberGenerator]::GetBytes($Length)
    }
}

function New-Certificate {
    param(
        [string]$Subject,
        [System.Security.Cryptography.RSA]$Key,
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$IssuerCert = $null,
        [System.Security.Cryptography.RSA]$IssuerKey = $null,
        [bool]$IsCA = $false,
        [int]$Days = $DefaultCertificateExpiration.TotalDays,
        [string]$OutFile = $null
    )
    Write-Host "Running New-Certificate($Subject)"

    $req = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new($Subject, [System.Security.Cryptography.RSA]$Key, [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)

    $SubjectKeyIdentifierExtension = [System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension]::new($req.PublicKey, $false)
    $req.CertificateExtensions.Add($SubjectKeyIdentifierExtension)

    if ($IsCA) {
        $BasicConstraintExtension = New-Object System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension -ArgumentList $true, $false, 0, $true
        $req.CertificateExtensions.Add($BasicConstraintExtension)
    } else {
        $BasicConstraintExtension = New-Object System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension -ArgumentList $false, $false, 0, $false
        $req.CertificateExtensions.Add($BasicConstraintExtension)
    }

    if ($IsCA) {
        $KeyUsageExtension = [System.Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new( `
            [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature `
            -bor [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::CrlSign `
            -bor [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyCertSign,
            $true)
    } else {
        $KeyUsageExtension = [System.Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new( `
            [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature `
            -bor [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyEncipherment,
            $true)
    }

    $req.CertificateExtensions.Add($KeyUsageExtension)

    if (-not $IsCA) {
        $EkuOids = [System.Security.Cryptography.OidCollection]::new()
        [void]$EkuOids.Add(
            [System.Security.Cryptography.Oid]::new("1.3.6.1.5.5.7.3.2") # clientAuth = 1.3.6.1.5.5.7.3.2
        ) 
        $EnhancedKeyUsageExtension = [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($EkuOids, $false)
        $req.CertificateExtensions.Add($EnhancedKeyUsageExtension)
    }

    $NotBefore = [datetime]::UtcNow
    $NotAfter = [datetime]::UtcNow.AddDays($Days)

    if ($IssuerCert -eq $null) {
        # Self-signed (Root CA)
        $NewCertificate = $req.CreateSelfSigned($NotBefore, $NotAfter)

        if ($OutFile -ne $null -and $OutFile -ne "") {
            Export-X509CertificateToPemFile -Cert $NewCertificate -Path $OutFile
        }

        return $NewCertificate
    } else {
        # Signed by issuer (Intermediate or Device)

        # Adjust NotBefore and NotAfter to match issuer certificate.
        if ($null -ne $IssuerCert.NotBefore -and $NotBefore -lt $IssuerCert.NotBefore) {
            $NotBefore = $IssuerCert.NotBefore
        }

        if ($null -ne $IssuerCert.NotAfter -and $NotAfter -gt $IssuerCert.NotAfter) {
            $NotAfter = $IssuerCert.NotAfter
        }

        # Serial number must be random bytes (8–20 bytes is typical)
        $SerialNumber = New-RandomNumber -Length 16

        # Use the issuer *private key* for signing
        $SignatureGenerator = [System.Security.Cryptography.X509Certificates.X509SignatureGenerator]::CreateForRSA(
            $IssuerKey,
            [System.Security.Cryptography.RSASignaturePadding]::Pkcs1
        )

        # Use issuer subject name from the issuer cert
        $NewCertificate = $req.Create(
            $IssuerCert.SubjectName,
            $SignatureGenerator,
            $NotBefore,
            $NotAfter,
            $SerialNumber
        )

        if ($null -ne $OutFile -and $OutFile -ne "") {
            Export-X509CertificateToPemFile -Cert $NewCertificate -Path $OutFile
        }

        return $NewCertificate
    }
}

function Export-X509CertificateToPem {
    param([System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate)

    if ($PSVersionTable.PSVersion.Major -lt 7) {
        $CertificatePemHex = [Convert]::ToBase64String($Certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert), 'InsertLineBreaks')
        return "-----BEGIN CERTIFICATE-----`n$CertificatePemHex`n-----END CERTIFICATE-----"
    } else {
        return $Certificate.ExportCertificatePem()
    }
}

function Export-X509CertificateToPemFile {
    param([System.Security.Cryptography.X509Certificates.X509Certificate2]$Cert, [string]$Path)
    $pem = Export-X509CertificateToPem -Certificate $Cert
    Write-Host "Exporting certificate to $Path"
    Set-FileContent -Path $Path -Content $pem
}



# Generic Types
class RsaPrivateKeyInfo {
    [System.Security.Cryptography.RSA]$PrivateKey = $null

    RsaPrivateKeyInfo(
        [System.Security.Cryptography.RSA]$PrivateKey
    ) {
        $this.PrivateKey = $PrivateKey
    }

    [System.Security.Cryptography.RSA]ToNativeRsaKey() {
        return $this.PrivateKey
    }

    [string]ToPem() {
        return Export-Pkcs8PrivateKeyPem -Key $this.PrivateKey
    }
}

class X509CertificateInfo {
    [RsaPrivateKeyInfo]$PrivateKey = $null
    [System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate = $null

    X509CertificateInfo(
        [System.Security.Cryptography.RSA]$PrivateKey,
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate
    ) {
        $this.PrivateKey = [RsaPrivateKeyInfo]::new($PrivateKey)
        $this.Certificate = $Certificate
    }

    [string]GetThumbprint() {
        return $this.Certificate.Thumbprint
    }

    [System.Security.Cryptography.X509Certificates.X509Certificate2]ToNativeX509Certificate2() {
        return $this.Certificate
    }

    [string]ToPem() {
        return Export-X509CertificateToPem -Certificate $this.Certificate
    }

    [void]ExportToPemFile([string]$Path) {
        Export-X509CertificateToPemFile -Cert $this.Certificate -Path $Path
    }
}


# Azure IoT Types
class IotHubSymmetricKeyIdentityInfo {
    [string]$Id = $null
    [string]$PrimaryKey = $null
    [string]$SecondaryKey = $null
    [string]$PrimaryConnectionString = $null
    [string]$SecondaryConnectionString = $null

    IotHubSymmetricKeyIdentityInfo(
        [string]$Id,
        [string]$PrimaryKey,
        [string]$SecondaryKey,
        [string]$PrimaryConnectionString,
        [string]$SecondaryConnectionString
    ) {
        $this.Id = $Id
        $this.PrimaryKey = $PrimaryKey
        $this.SecondaryKey = $SecondaryKey
        $this.PrimaryConnectionString = $PrimaryConnectionString
        $this.SecondaryConnectionString = $SecondaryConnectionString
    }
}

class IotHubX509IdentityInfo {
    [string]$Id = $null
    [string]$ConnectionString = $null
    [X509CertificateInfo]$PrimaryCertificate = $null
    [X509CertificateInfo]$SecondaryCertificate = $null

    IotHubX509IdentityInfo(
        [string]$Id,
        [string]$ConnectionString,
        [X509CertificateInfo]$PrimaryCertificate,
        [X509CertificateInfo]$SecondaryCertificate
    ) {
        $this.Id = $Id
        $this.ConnectionString = $ConnectionString
        $this.PrimaryCertificate = $PrimaryCertificate
        $this.SecondaryCertificate = $SecondaryCertificate
    }
}

class DpsSymmetricKeyIdentityInfo {
    [string]$Id
    [string]$PrimaryKey
    [string]$SecondaryKey

    DpsSymmetricKeyIdentityInfo(
        [string]$Id,
        [string]$PrimaryKey,
        [string]$SecondaryKey
    ) {
        $this.Id = $Id
        $this.PrimaryKey = $PrimaryKey
        $this.SecondaryKey = $SecondaryKey
    }

    [DpsSymmetricKeyIdentityInfo]DeriveKeysForDevice([string]$DeviceId) {
        return [DpsSymmetricKeyIdentityInfo]::new(
            $DeviceId,
            $(New-DpsDerivedSymmetricKey -SymmetricKey $this.PrimaryKey -DeviceId $DeviceId),
            $(New-DpsDerivedSymmetricKey -SymmetricKey $this.SecondaryKey -DeviceId $DeviceId)
        )
    }
}

class DpsX509IdentityInfo {
    [string]$Id
    [X509CertificateInfo]$Certificate

    DpsX509IdentityInfo(
        [string]$Id,
        [X509CertificateInfo]$Certificate
     ) {
        $this.Id = $Id
        $this.Certificate = $Certificate
     }
}

class DpsSymmetricKeyIndividualEnrollmentInfo : DpsSymmetricKeyIdentityInfo {
    DpsSymmetricKeyIndividualEnrollmentInfo(
        [string]$Id,
        [string]$PrimaryKey,
        [string]$SecondaryKey
    ) : base($Id, $PrimaryKey, $SecondaryKey) { }
}

class DpsX509IndividualEnrollmentInfo : DpsX509IdentityInfo {
    DpsX509IndividualEnrollmentInfo(
        [string]$Id,
        [X509CertificateInfo]$Certificate
     ) : base($Id, $Certificate) { }
}

class DpsSymmetricKeyEnrollmentGroupInfo : DpsSymmetricKeyIdentityInfo {
    [DpsSymmetricKeyIdentityInfo[]]$Identities = @()

    DpsSymmetricKeyEnrollmentGroupInfo(
        [string]$Id,
        [string]$PrimaryKey,
        [string]$SecondaryKey
    ) : base($Id, $PrimaryKey, $SecondaryKey) { }

     [DpsSymmetricKeyIdentityInfo]AddIdentity([string]$DeviceId) {
        $DeviceIdentityInfo = $this.DeriveKeysForDevice($DeviceId)

        $this.Identities += $DeviceIdentityInfo

        return $DeviceIdentityInfo
     }
}

class DpsX509EnrollmentGroupInfo : DpsX509IdentityInfo {
    [DpsX509IdentityInfo[]]$Identities = @()

    DpsX509EnrollmentGroupInfo(
        [string]$Id,
        [X509CertificateInfo]$Certificate
     ) : base($Id, $Certificate) { }

     [DpsX509IdentityInfo]AddIdentity([string]$DeviceId, [timespan]$CertificateExpiration) {
        $EnrollmentGroupPrivateKey = $this.Certificate.PrivateKey.ToNativeRsaKey()
        $EnrollmentGroupCertificate = $this.Certificate.ToNativeX509Certificate2()

        $DpsDevicePrivateKey = New-RsaPrivateKey
        $DpsDeviceCertificate = New-Certificate -Subject "CN=$DeviceId" -Key $DpsDevicePrivateKey -IssuerCert $EnrollmentGroupCertificate -IssuerKey $EnrollmentGroupPrivateKey -IsCA $false -Days $CertificateExpiration.TotalDays

        $DeviceIdentityInfo = [DpsX509IdentityInfo]::new(
            $DeviceId,
            [X509CertificateInfo]::new($DpsDevicePrivateKey, $DpsDeviceCertificate)
        )

        $this.Identities += $DeviceIdentityInfo

        return $DeviceIdentityInfo
     }
}

class DpsEnrollmentsSet {
    [DpsSymmetricKeyIndividualEnrollmentInfo[]]$IndividualSymmetricKey = [DpsSymmetricKeyIndividualEnrollmentInfo[]]@()
    [DpsX509IndividualEnrollmentInfo[]]$IndividualX509 = [DpsX509IndividualEnrollmentInfo[]]@()
    [DpsSymmetricKeyEnrollmentGroupInfo[]]$GroupSymmetricKey = [DpsSymmetricKeyEnrollmentGroupInfo[]]@()
    [DpsX509EnrollmentGroupInfo[]]$GroupX509 = [DpsX509EnrollmentGroupInfo[]]@()
}

class DpsInfo {
    [string]$ResourceGroup = $null
    [string]$DeviceFqdn = $null
    [string]$ServiceFqdn = $null
    [string]$ConnectionString = $null
    [string]$IdScope = $null
    [X509CertificateInfo[]]$RootCaCertificates = @()
    [DpsEnrollmentsSet]$Enrollments = [DpsEnrollmentsSet]::new()

    [X509CertificateInfo]AddRootCaCertificate() {
        $DpsName = $this.ServiceFqdn.split(".")[0]
        $DpsRootCertificate = Add-DpsCertificate -ResourceGroup $this.ResourceGroup -DpsName $DpsName
        $this.RootCaCertificates += $DpsRootCertificate
        return $DpsRootCertificate
    }
}

class EventHubInfo {
    [string]$ConnectionString = $null
    [string]$CompatibleName = $null
    [int]$PartitionCount = 0
    [array]$ConsumerGroups = @()
}

class IotHubDeviceSet {
    [IotHubSymmetricKeyIdentityInfo[]]$SymmetricKey = @()
    [array]$X509Thumbprint = @()
    # $X509CA = @()
}

class IotHubInfo {
    [string]$ConnectionString = $null
    [EventHubInfo]$EventHub = [EventHubInfo]::new()
    [IotHubDeviceSet]$Devices = [IotHubDeviceSet]::new()
}

class TestEnvironmentInfo {
    [string]$AzureResourceGroup = $null

    [IotHubInfo]$IotHub = [IotHubInfo]::new()

    [DpsInfo]$Dps = [DpsInfo]::new()

    [string]$AzureAdrPolicyName = $null
}




function New-DpsDerivedSymmetricKey {
    param(
        $SymmetricKey = $null,
        $DeviceId = $null
    )

    $hmacsha256 = New-Object System.Security.Cryptography.HMACSHA256
    try {
        $hmacsha256.key = [Convert]::FromBase64String($SymmetricKey)
        $sig = $hmacsha256.ComputeHash([Text.Encoding]::ASCII.GetBytes($DeviceId))
        return [Convert]::ToBase64String($sig)
    } finally {
        $hmacsha256.Dispose()
    }
}


function Add-DpsCertificate {
    param(
        [string]$ResourceGroup,
        [string]$DpsName,
        [string]$Subject = $null,
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$IssuerCert = $null,
        [System.Security.Cryptography.RSA]$IssuerKey = $null,
        [timespan]$Expiration = $DefaultCertificateExpiration
    )

    if ($null -eq $Subject -or $Subject -eq "") {
        $Subject = "Azure IoT Test Certificate {0}" -f (New-Guid)
    }

    $DpsCertificateName = $Subject.Replace(" ", "-")
    $Subject = "CN=$Subject"

    Write-Host "Running Add-DpsCertificate($DpsCertificateName)"

    $PrivateKey = New-RsaPrivateKey
    $CertificatePath =  New-TempFile
    $Certificate = New-Certificate -Subject $Subject -Key $PrivateKey -IssuerCert $IssuerCert -IssuerKey $IssuerKey -IsCA $true -Days $Expiration.TotalDays -OutFile $CertificatePath

    az iot dps certificate create --dps-name $DpsName --resource-group $ResourceGroup --name $DpsCertificateName --path $CertificatePath | Out-Null
    Stop-OnError -Step "az iot dps certificate create"

    Remove-Item $CertificatePath

    $etag = az iot dps certificate show --dps-name $DpsName --resource-group $ResourceGroup --name $DpsCertificateName --query etag -o tsv
    Stop-OnError -Step "az iot dps certificate show"

    $DpsVerificationCodeInfo = az iot dps certificate generate-verification-code --dps-name $DpsName --resource-group $ResourceGroup --name $DpsCertificateName --etag $etag | ConvertFrom-Json
    Stop-OnError -Step "az iot dps certificate generate-verification-code"

    # Create verification cert
    $DpsVerificationCertificatePath = New-TempFile
    $DpsVerificationCertificateSubject = "CN=$($DpsVerificationCodeInfo.properties.verificationCode)"

    $DpsVerificationKey = New-RsaPrivateKey
    New-Certificate -Subject $DpsVerificationCertificateSubject -Key $DpsVerificationKey -IssuerCert $Certificate -IssuerKey $PrivateKey -IsCA $false -Days $Expiration.TotalDays -OutFile $DpsVerificationCertificatePath | Out-Null

    # Verify with DPS
    $etag = az iot dps certificate show --dps-name $DpsName --resource-group $ResourceGroup --name $DpsCertificateName --query etag -o tsv
    Stop-OnError -Step "az iot dps certificate show"

    az iot dps certificate verify --dps-name $DpsName --resource-group $ResourceGroup --name $DpsCertificateName --path $DpsVerificationCertificatePath --etag $etag | Out-Null
    Stop-OnError -Step "az iot dps certificate verify"

    Remove-Item $DpsVerificationCertificatePath

    return [X509CertificateInfo]::new($PrivateKey, $Certificate)
}

function Add-DpsSymmetricKeyIndividualEnrollment {
    param(
        [string]$ResourceGroup = $null,
        [string]$DpsName = $null,
        [string]$EnrollmentId = $null,
        [string]$AdrPolicyName = $null
    )

    Write-Host "Creating Azure DPS symmetric-key individual enrollment ($EnrollmentId; $AdrPolicyName)."

    if ($AdrPolicyName -eq $null -or $AdrPolicyName -eq "") {
        $EnrollmentInfo = az iot dps enrollment create --dps-name $DpsName --resource-group $ResourceGroup --at symmetricKey --enrollment-id $EnrollmentId  | ConvertFrom-Json
    } else {
        $EnrollmentInfo = az iot dps enrollment create --dps-name $DpsName --resource-group $ResourceGroup --at symmetricKey --enrollment-id $EnrollmentId --credential-policy $AdrPolicyName | ConvertFrom-Json
    }

    Stop-OnError -Step "Create an Azure DPS symmetric-key individual enrollment ($EnrollmentId; $AdrPolicyName)"

    return [DpsSymmetricKeyIndividualEnrollmentInfo]::new(
        $EnrollmentId,
        $EnrollmentInfo.attestation.symmetricKey.primaryKey,
        $EnrollmentInfo.attestation.symmetricKey.secondaryKey
    )
}

function Add-DpsX509IndividualEnrollment {
    param(
        [string]$ResourceGroup = $null,
        [string]$DpsName = $null,
        [string]$EnrollmentId = $null,
        [string]$AdrPolicyName = $null
    )

    Write-Host "Creating Azure DPS x509 individual enrollment ($EnrollmentId; $AdrPolicyName; $CertificatesDir; $PrivateKeysDir)."

    $DpsDevicePrivateKey = New-RsaPrivateKey
    $DpsDeviceCertificate = New-Certificate -Subject "CN=$EnrollmentId" -Key $DpsDevicePrivateKey -IssuerCert $null -IssuerKey $null -IsCA $false -Days $DefaultCertificateExpiration.TotalDays

    $DpsDeviceCertificatePath = New-TempFile
    Export-X509CertificateToPemFile -Cert $DpsDeviceCertificate -Path $DpsDeviceCertificatePath

    if ($AdrPolicyName -eq $null -or $AdrPolicyName -eq "") {
        az iot dps enrollment create --dps-name $DpsName --resource-group $ResourceGroup --at x509 --enrollment-id $EnrollmentId --cp $DpsDeviceCertificatePath | Out-Null
    } else {
        az iot dps enrollment create --dps-name $DpsName --resource-group $ResourceGroup --at x509 --enrollment-id $EnrollmentId --cp $DpsDeviceCertificatePath --credential-policy $AdrPolicyName | Out-Null
    }

    Stop-OnError -Step "Create an Azure DPS x509 individual enrollment ($EnrollmentId; $AdrPolicyName)"

    Remove-Item $DpsDeviceCertificatePath

    return [DpsX509IndividualEnrollmentInfo]::new(
        $EnrollmentId,
        [X509CertificateInfo]::new($DpsDevicePrivateKey, $DpsDeviceCertificate)
    )
}

function Add-DpsSymmetricKeyEnrollmentGroup {
    param(
        [string]$ResourceGroup = $null,
        [string]$DpsName = $null,
        [string]$EnrollmentId = $null,
        [string]$AdrPolicyName = $null
    )

    Write-Host "Creating Azure DPS symmetric-key enrollment group ($EnrollmentId; $AdrPolicyName)."

    if ($AdrPolicyName -eq $null -or $AdrPolicyName -eq "") {
        $EnrollmentInfo = az iot dps enrollment-group create --dps-name $DpsName --resource-group $ResourceGroup --enrollment-id $EnrollmentId | ConvertFrom-Json
    } else {
        $EnrollmentInfo = az iot dps enrollment-group create --dps-name $DpsName --resource-group $ResourceGroup --enrollment-id $EnrollmentId --credential-policy $AdrPolicyName | ConvertFrom-Json
    }

    Stop-OnError -Step "Create an Azure Device Provisioning symmetric-key enrollment group ($EnrollmentId; $AdrPolicyName)"

    return [DpsSymmetricKeyEnrollmentGroupInfo]::new(
        $EnrollmentId,
        $EnrollmentInfo.attestation.symmetricKey.primaryKey,
        $EnrollmentInfo.attestation.symmetricKey.secondaryKey
    )
}

function Add-DpsX509EnrollmentGroup {
    param(
        [string]$ResourceGroup = $null,
        [string]$DpsName = $null,
        [string]$EnrollmentId = $null,
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$IssuerCertificate = $null,
        [System.Security.Cryptography.RSA]$IssuerPrivateKey = $null,
        [string]$IotHubFqdn = $null,
        [string]$AdrPolicyName = $null,
        [timespan]$CertificateExpiration = $DefaultCertificateExpiration
    )

    Write-Host "Creating Azure DPS x509 enrollment group ($EnrollmentId; $AdrPolicyName)."

    $ICA = Add-DpsCertificate -ResourceGroup $ResourceGroup -DpsName $DpsName -Subject $EnrollmentId -IssuerCert $IssuerCertificate -IssuerKey $IssuerPrivateKey -Expiration $CertificateExpiration

    $ICACertificatePath = New-TempFile
    $ICA.ExportToPemFile($ICACertificatePath)

    if ($AdrPolicyName -eq $null -or $AdrPolicyName -eq "") {
        az iot dps enrollment-group create --dps-name $DpsName --resource-group $ResourceGroup --enrollment-id $EnrollmentId --ap static --cp $ICACertificatePath --provisioning-status enabled --iot-hubs $IotHubFqdn  | Out-Null
    } else {
        az iot dps enrollment-group create --dps-name $DpsName --resource-group $ResourceGroup --enrollment-id $EnrollmentId --ap static --cp $ICACertificatePath --provisioning-status enabled --iot-hubs $IotHubFqdn --credential-policy $AdrPolicyName | Out-Null
    }

    Stop-OnError -Step "Create an Azure Device Provisioning x509 enrollment group ($EnrollmentId; $AdrPolicyName)"

    Remove-Item $ICACertificatePath

    return [DpsX509EnrollmentGroupInfo]::new($EnrollmentId, $ICA)
}

function New-AzureResourceGroupName {
    param([string]$Prefix = "rg-", [string]$OutFile = $null)

    $ResourceGroupName = $Prefix + $(New-Guid -NoDashes)

    if ($null -ne $OutFile -and $OutFile -ne "") {
        $OutFileDir = Split-Path -Path $OutFile -Parent
        if ($OutFileDir -ne "" -and $(Test-Path $OutFileDir) -eq $false) {
            New-Item -ItemType Directory -Force -Path $OutFileDir | Out-Null
        }

        Set-FileContent -Path $OutFile -Content $ResourceGroupName

    }

    return $ResourceGroupName
}

function New-AzIotTestEnvironment {
    <#
    .SYNOPSIS
    Creates a new set of Azure Resources for testing Azure IoT scenarios, including an IoT Hub and optionally a Device Provisioning Service, with different types of enrollments and devices.

    .DESCRIPTION
    Creates a new set of Azure Resources for testing Azure IoT scenarios, including an IoT Hub and optionally a Device Provisioning Service, with different types of enrollments and devices.

    .PARAMETER AzureLocation
    Specifies the Azure location for the resources. Default is "centraluseuap".
    .PARAMETER AzureSubscriptionId
    Specifies the Azure subscription ID. If not provided, the current Azure CLI session default subscription will be used.
    .PARAMETER ResourceGroup
    Specifies the name of the resource group. If not provided, a new resource group will be created.
    .PARAMETER ResourceGroupTags
    Specify a hashtable with the key/value pairs to use as tags for Azure Resource Group creation.
    See `az group create --tags` for more details.
    .PARAMETER DpsName
    Specifies the name of the Device Provisioning Service. If not provided, a new DPS will be created.
    .PARAMETER IotHubName
    Specifies the name of the IoT Hub. If not provided, a new IoT Hub will be created.
    .PARAMETER IotHubDomainName
    Specifies the domain name of the IoT Hub. Default is "azure-devices.net".
    .PARAMETER StorageAccountName
    Specifies the name of the Storage Account. If not provided, a new Storage Account will be created.
    .PARAMETER DpsSymmKeyIndividualEnrollments
    Specifies the number of symmetric key individual enrollments to create in DPS. Default is 0.
    .PARAMETER DpsX509IndividualEnrollments
    Specifies the number of x509 individual enrollments to create in DPS. Default is 0.
    .PARAMETER DpsSymmKeyGroupEnrollmentDevices
    Specifies the number of devices to create under a symmetric key enrollment group in DPS. Default is 0.
    .PARAMETER DpsX509GroupEnrollmentDevices
    Specifies the number of devices to create under an x509 enrollment group in DPS. Default is 1.
    .PARAMETER IotHubSymmKeyDevices
    Specifies the number of symmetric key devices to create in IoT Hub. Default is 1.
    .PARAMETER IotHubX509ThumbprintDevices
    Specifies the number of x509 thumbprint devices to create in IoT Hub. Default is 1.
    .PARAMETER IotHubX509CADevices
    Specifies the number of x509 CA devices to create in IoT Hub. Default is 0.
    .PARAMETER EnableFileUpload
    Specifies whether to enable file upload in IoT Hub. Default is false.
    .PARAMETER NoDps
    Specifies whether to skip creating a Device Provisioning Service. Default is false.
    .PARAMETER EnableCertificateManagement
    Specifies whether to enable certificate management in IoT Hub and DPS using Azure Device Registration (ADR). Default is false.

    .OUTPUTS
    A custom object containing information about the created Azure resources and devices, including connection strings, certificate paths, and enrollment details.

    .EXAMPLE
    PS> $TestEnvInfo = New-AzIotTestEnvironment

    This command creates a new Azure IoT test environment in the default location with 1 x509 group enrollment device in DPS and 1 symmetric key device and 1 x509 thumbprint device in IoT Hub, without enabling certificate management nor file upload on Azure IoT Hub.

    .EXAMPLE
    PS> $TestEnvInfo = New-AzIotTestEnvironment -AzureLocation "eastus2" -DpsSymmKeyIndividualEnrollments 2 -DpsX509IndividualEnrollments 1 -DpsSymmKeyGroupEnrollmentDevices 2 -DpsX509GroupEnrollmentDevices 2 -IotHubSymmKeyDevices 2 -IotHubX509ThumbprintDevices 1 -IotHubX509CADevices 1
    
    This command creates a new Azure IoT test environment in the "eastus2" location with 2 symmetric key individual enrollments, 1 x509 individual enrollment, 2 devices under a symmetric key enrollment group, 2 devices under an x509 enrollment group in DPS, and 2 symmetric key devices, 1 x509 thumbprint device, and 1 x509 CA device in IoT Hub.

    .EXAMPLE
    PS> $TestEnvInfo = New-AzIotTestEnvironment -EnableCertificateManagement
    
    This command creates a new Azure IoT test environment with an IoT Hub and Device Provisioning Service that has certificate management enabled using Azure Device Registration (ADR).
    #>
    param(
        [string]$AzureLocation = "centraluseuap", # Other locations (e.g.): eastus2euap, westus2, ...
        [string]$AzureSubscriptionId = $null,
        [string]$ResourceGroup = $(New-AzureResourceGroupName),
        [Hashtable]$ResourceGroupTags = $null,
        [string]$DpsName       = "dps-$(New-Guid -NoDashes)",
        [string]$IotHubName    = "iothub-$(New-Guid -NoDashes)",
        [string]$IotHubDomainName = "azure-devices.net",
        [string]$StorageAccountName = "teststoacc$(New-Guid -NoDashes)".Substring(0, 24), # Max size of Storage Account name
        [int]$DpsSymmKeyIndividualEnrollments = 0,
        [int]$DpsX509IndividualEnrollments = 0,
        [int]$DpsSymmKeyGroupEnrollmentDevices = 0,
        [int]$DpsX509GroupEnrollmentDevices = 0,
        [int]$IotHubSymmKeyDevices = 0,
        [int]$IotHubX509ThumbprintDevices = 0,
        [int]$IotHubX509CADevices = 0,
        [switch]$EnableFileUpload,
        [switch]$NoDps,
        [switch]$EnableCertificateManagement
    )

    $IotHubFqdn = "$($IotHubName).$($IotHubDomainName)"

    # Login to Azure if not already
    $null = & az account show 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Not logged in to Azure. Running 'az login'..."
        & az login --use-device-code
        if ($LASTEXITCODE -ne 0) {
            throw "Azure login failed."
        }
    }
    else {
        Write-Host "Already logged in to Azure."
    }

    $AzureAccount = az account show | ConvertFrom-Json
    $IsAzureAccountServicePrincipal = $AzureAccount.user.type -eq "servicePrincipal"

    # Subscription id...
    if ($AzureSubscriptionId -eq $null -or $AzureSubscriptionId -imatch "[ `t]*") {
        Stop-OnError -Step "Get Azure account information"
        $AzureSubscriptionId = $AzureAccount.id
    } else {
        $discard = az account set --subscription "$AzureSubscriptionId" --only-show-errors 2>$null
        Stop-OnError -Step "Set Azure subscription"
    }

    # Add Azure IoT extension if not already added (required for some az iot commands, e.g. az iot adr ns create)
    # TODO: install non-preview version after GA.
    $AzCliAzureIotExtension = $(az extension list | Convertfrom-json | ?{$_.name -eq "azure-iot"})

    # ADR credential-policy support requires preview version >= 0.30.0b2.
    # Remove any existing version (stable or older preview) and install the required preview.
    $RequiredVersion = "0.30.0b2"
    if ($AzCliAzureIotExtension -ne $null -and $AzCliAzureIotExtension.version -ne $RequiredVersion) {
        Write-Host "Azure IoT extension found (version $($AzCliAzureIotExtension.version)), upgrading to $RequiredVersion..."
        az extension remove --name azure-iot --only-show-errors | Out-Null
        Stop-OnError -Step "Remove old Azure IoT extension"
        $AzCliAzureIotExtension = $null
    }

    if ($AzCliAzureIotExtension -eq $null) {
        Write-Host "Installing Azure IoT extension (version $RequiredVersion)."
        az extension add --name azure-iot --version $RequiredVersion --allow-preview --only-show-errors | Out-Null
        Stop-OnError -Step "Install Azure IoT extension"
    }

    # Create resource group (if does not exist).
    $ResouceGroupExists = az group exists --name $ResourceGroup -o tsv
    if ($ResouceGroupExists -eq 'true') {
       $AzureResourceGroup = az group show --name "$ResourceGroup" | ConvertFrom-Json
    } else {
        $ResouceGroupTagsString = $(Join-Hashtable -Hashtable $ResourceGroupTags)

        Write-Host "Creating Azure resource group ($ResourceGroup; $ResouceGroupTagsString)"

        if ($null -ne $ResourceGroupTags -and $ResourceGroupTags.Count -gt 0) {
            $AzureResourceGroup = az group create --name "$ResourceGroup" --location "$AzureLocation" --tags "$ResouceGroupTagsString" 2>$null | ConvertFrom-json 
        } else {
            $AzureResourceGroup = az group create --name "$ResourceGroup" --location "$AzureLocation" 2>$null | ConvertFrom-json 
        }

        Stop-OnError -Step "Create Azure resource group"
    }

    $TestEnvInfo = [TestEnvironmentInfo]::new()

    if ($EnableCertificateManagement -eq $true) {
        $AzureIotHubAppId = "89d10474-74af-4874-99a7-c23c2f643083" # Azure IoT Hub application ID (same for all tenants)
        $AzureIotHubObjectId = "0aab4033-4ad9-4b0b-9934-542334eceffb" # Manually obtained...

        $ResouceGroupScope = "/subscriptions/$AzureSubscriptionId/resourceGroups/$ResourceGroup"
        Write-Host "Creating Azure role assignment for certificate management (scope=$ResouceGroupScope)"
        if ($IsAzureAccountServicePrincipal) {
            az role assignment create --assignee-object-id $AzureIotHubObjectId --assignee-principal-type ServicePrincipal --role Contributor --scope "$ResouceGroupScope" --only-show-errors | Out-Null
        } else {
            az role assignment create --assignee $AzureIotHubAppId --role Contributor --scope "$ResouceGroupScope" --only-show-errors | Out-Null
        }
        Stop-OnError -Step "Create Azure role assignment for certificate management"

        $CertMgmtUserIdentity = "$($ResourceGroup)cmuid"
        Write-Host "Creating User-Assigned Managed Identity (UAMI) ($CertMgmtUserIdentity)"
        $AzureCertMgmtIdentity = az identity create --name "$CertMgmtUserIdentity" --resource-group "$ResourceGroup" --location "$AzureLocation" 2>$null | ConvertFrom-Json
        Stop-OnError -Step "Create User-Assigned Managed Identity (UAMI)"

        $AzureAdrNamespaceName = "azure-adr-ns"
        $AzureAdrPolicyName = "azure-adr-policy"
        Write-Host "Creating ADR Namespace (ns=$AzureAdrNamespaceName; policy=$AzureAdrPolicyName)"
        $AzureAdrNamespace = az iot adr ns create --name "$AzureAdrNamespaceName" --enable-certificate-management --resource-group "$ResourceGroup" --location "$AzureLocation" --policy-name "$AzureAdrPolicyName" 2>$null | ConvertFrom-Json
        Stop-OnError -Step "Create ADR Namespace"    

        Write-Host "Assigning ADR custom role to UAMI (a5c3590a-3a1a-4cd4-9648-ea0a32b15137)"
        az role assignment create --assignee "$($AzureCertMgmtIdentity.principalId)" --role "a5c3590a-3a1a-4cd4-9648-ea0a32b15137" --scope "$($AzureAdrNamespace.id)" --only-show-errors | Out-Null
        Stop-OnError -Step "Assign ADR custom role 1 to UAMI"

        Write-Host "Assigning ADR custom role to UAMI (547f7f0a-69c0-4807-bd9e-0321dfb66a84)"
        az role assignment create --assignee "$($AzureCertMgmtIdentity.principalId)" --role "547f7f0a-69c0-4807-bd9e-0321dfb66a84" --scope "$($AzureAdrNamespace.id)" --only-show-errors | Out-Null
        Stop-OnError -Step "Assign ADR custom role 2 to UAMI"

        Write-Host "Creating Azure IoT Hub ($IotHubName, with certificate management support)"
        $AzureIoTHub = az iot hub create --name "$IotHubName" --resource-group "$ResourceGroup" --location "$AzureLocation" --sku GEN2 `
            --mi-user-assigned "$($AzureCertMgmtIdentity.id)" --ns-resource-id "$($AzureAdrNamespace.id)" --ns-identity-id "$($AzureCertMgmtIdentity.id)" | ConvertFrom-Json
        Stop-OnError -Step "Create Azure IoT Hub (with certificate management support)"

        Write-Host "Assigning Contributor role on Azure IoT for ADR principal"
        az role assignment create --assignee "$($AzureAdrNamespace.identity.principalId)" --role "Contributor" --scope "$($AzureIoTHub.id)" --only-show-errors | Out-Null
        Stop-OnError -Step "Assign Contributor role on Azure IoT for ADR principal"

        Write-Host "Assigning IoT Hub Registry Contributor role on Azure IoT for ADR principal"
        az role assignment create --assignee "$($AzureAdrNamespace.identity.principalId)" --role "IoT Hub Registry Contributor" --scope "$($AzureIoTHub.id)" --only-show-errors | Out-Null
        Stop-OnError -Step "Assign IoT Hub Registry Contributor role on Azure IoT for ADR principal"
        
        if ($NoDps -eq $false) {
            Write-Host "Creating Device Provisioning Service with ADR integration ($DpsName)"
            $AzureDps = az iot dps create --name "$DpsName" --resource-group "$ResourceGroup" --location "$AzureLocation" `
                --mi-user-assigned "$($AzureCertMgmtIdentity.id)" --ns-resource-id "$($AzureAdrNamespace.id)" --ns-identity-id "$($AzureCertMgmtIdentity.id)" --only-show-errors | ConvertFrom-Json
            Stop-OnError -Step "Create Device Provisioning Service with ADR integration"
        }
    } else {
        Write-Host "Creating Azure IoT Hub ($IotHubName)."
        $AzureIoTHub = az iot hub create --name "$IotHubName" --resource-group "$ResourceGroup" --location "$AzureLocation" --mintls "1.2" | ConvertFrom-Json
        Stop-OnError -Step "Create Azure IoT Hub"

        if ($NoDps -eq $false) {
            Write-Host "Creating Azure Device Provisioning Service ($DpsName)."
            $AzureDps = az iot dps create --name "$DpsName" --resource-group "$ResourceGroup" --location "$AzureLocation" | ConvertFrom-Json
            Stop-OnError -Step "Create Device Provisioning Service"
        }
    }

    if ($NoDps -eq $false) {    
        Write-Host "Linking Azure IoT Hub ($IotHubName) to Azure Device Provisioning service ($DpsName)"
        az iot dps linked-hub create --dps-name "$DpsName" --resource-group "$ResourceGroup" --hub-name "$IotHubName" | Out-Null
        Stop-OnError -Step "Link Azure IoT Hub to Azure Device Provisioning service"

        # Step was put here to optimize if blocks, since it's common down.
        Write-Host "Creating DPS Root Certificate"
        $DpsRootCertificate = Add-DpsCertificate -ResourceGroup $ResourceGroup -DpsName $DpsName
    }

    if ($EnableCertificateManagement -eq $true) {
        Write-Host "Syncing ADR credentials ($AzureAdrNamespaceName)."
        az iot adr ns credential sync --ns "$AzureAdrNamespaceName" --resource-group "$ResourceGroup" --only-show-errors | Out-Null
        Stop-OnError -Step "Sync ADR credentials"
    } else {
        $AzureAdrPolicyName = $null # Used below on enrollment creation.
    }

    # Create IoT Hub Devices
    for ($i = 0; $i -lt $IotHubSymmKeyDevices; $i++) {
        $IotHubDeviceId = "sk-$(New-Guid -NoDashes)"

        Write-Host "Creating Azure IoT Hub symmetric-key device ($IotHubDeviceId)"
        $IotHubDeviceInfo = az iot hub device-identity create --resource-group $ResourceGroup --hub-name $IotHubName --device-id $IotHubDeviceId | ConvertFrom-Json
        Stop-OnError -Step "Create Azure IoT Hub symmetric-key device ($IotHubDeviceId)"
        $PrimaryConnectionString = az iot hub device-identity connection-string show --resource-group $ResourceGroup --hub-name $IotHubName -d $IotHubDeviceId --kt primary | ConvertFrom-Json
        Stop-OnError -Step "Get Azure IoT Hub device primary connection-string ($IotHubDeviceId)"
        $SecondaryConnectionString = az iot hub device-identity connection-string show --resource-group $ResourceGroup --hub-name $IotHubName -d $IotHubDeviceId --kt secondary | ConvertFrom-Json
        Stop-OnError -Step "Get Azure IoT Hub device secondary connection-string ($IotHubDeviceId)"

        $DeviceIdentity = [IotHubSymmetricKeyIdentityInfo]::new(
            $IotHubDeviceId,
            $IotHubDeviceInfo.authentication.symmetricKey.primaryKey,
            $IotHubDeviceInfo.authentication.symmetricKey.secondaryKey,
            $PrimaryConnectionString.connectionString,
            $SecondaryConnectionString.connectionString            
        )

        $TestEnvInfo.IotHub.Devices.SymmetricKey += $DeviceIdentity
    }

    for ($i = 0; $i -lt $IotHubX509ThumbprintDevices; $i++) {
        $IotHubDeviceId = "x509tp-$(New-Guid -NoDashes)"
        $CertificateSubjectName = "C=US, ST=Washington, L=Redmond, O=Company, OU=Org, CN=www.company.com"

        $IotHubDevicePrivateKey = New-RsaPrivateKey
        $IotHubDevicePrimaryCertificate = New-Certificate -Subject $CertificateSubjectName -Key $IotHubDevicePrivateKey
        $IotHubDeviceSecondaryCertificate = New-Certificate -Subject $CertificateSubjectName -Key $IotHubDevicePrivateKey

        Write-Host "Creating Azure IoT Hub x509 thumbprint device ($IotHubDeviceId)"
        $IotHubDeviceInfo = az iot hub device-identity create --resource-group $ResourceGroup --hub-name $IotHubName --device-id $IotHubDeviceId `
            --am x509_thumbprint --ptp $IotHubDevicePrimaryCertificate.Thumbprint --stp $IotHubDeviceSecondaryCertificate.Thumbprint | ConvertFrom-Json
        Stop-OnError -Step "Create Azure IoT Hub x509 thumbprint device ($IotHubDeviceId)"

        $ConnectionString = az iot hub device-identity connection-string show --resource-group $ResourceGroup --hub-name $IotHubName -d $IotHubDeviceId | ConvertFrom-Json
        Stop-OnError -Step "Get Azure IoT Hub device connection-string ($IotHubDeviceId)"

        $DeviceIdentity = [IotHubX509IdentityInfo]::new(
            $IotHubDeviceId,
            $ConnectionString.connectionString,
            [X509CertificateInfo]::new($IotHubDevicePrivateKey, $IotHubDevicePrimaryCertificate),
            [X509CertificateInfo]::new($IotHubDevicePrivateKey, $IotHubDeviceSecondaryCertificate)
        )

        $TestEnvInfo.IotHub.Devices.X509Thumbprint += $DeviceIdentity
    }
    # TODO: implement this...
    # $IotHubX509CADevices = 0
    # IotHubX509CADevices = @()
    # $TestEnvInfo.IotHub.Devices.X509CA = @()

    $DpsSymmKeyEnrollmentIdPrefix = "test-enrollment-sk"
    $DpsX509EnrollmentIdPrefix = "test-enrollment-x509"

    # Create all enrollments 
    if ($NoDps -eq $false) {
        for ($i = 0; $i -lt $DpsSymmKeyIndividualEnrollments; $i++) {
            $EnrollmentId = "$DpsSymmKeyEnrollmentIdPrefix-$i"
            $EnrollmentInfo = Add-DpsSymmetricKeyIndividualEnrollment -ResourceGroup $ResourceGroup -DpsName $DpsName -EnrollmentId $EnrollmentId -AdrPolicyName $AzureAdrPolicyName

            $TestEnvInfo.Dps.Enrollments.IndividualSymmetricKey += $EnrollmentInfo
        }

        for ($i = 0; $i -lt $DpsX509IndividualEnrollments; $i++) {
            $EnrollmentId = "$DpsX509EnrollmentIdPrefix-$i"
            $EnrollmentInfo = Add-DpsX509IndividualEnrollment -ResourceGroup $ResourceGroup -DpsName $DpsName -EnrollmentId $EnrollmentId -AdrPolicyName $AzureAdrPolicyName

            $TestEnvInfo.Dps.Enrollments.IndividualX509 += $EnrollmentInfo
        }

        if ($DpsSymmKeyGroupEnrollmentDevices -gt 0) {
            $EnrollmentId = "$DpsSymmKeyEnrollmentIdPrefix-group"
            $SKEnrollmentGroupInfo = Add-DpsSymmetricKeyEnrollmentGroup -ResourceGroup $ResourceGroup -DpsName $DpsName -EnrollmentId $EnrollmentId -AdrPolicyName $AzureAdrPolicyName

            $TestEnvInfo.Dps.Enrollments.GroupSymmetricKey += $SKEnrollmentGroupInfo

            for ($i = 0; $i -lt $DpsSymmKeyGroupEnrollmentDevices; $i++) {
                $SKEnrollmentGroupInfo.AddIdentity("group-prov-sk-$i") | Out-Null
            }
        }

        if ($DpsX509GroupEnrollmentDevices -gt 0) {
            $EnrollmentId = "$DpsX509EnrollmentIdPrefix-group"
            $X509EnrollmentGroupInfo = Add-DpsX509EnrollmentGroup -ResourceGroup $ResourceGroup -DpsName $DpsName -EnrollmentId $EnrollmentId -AdrPolicyName $AzureAdrPolicyName -IssuerCertificate $DpsRootCertificate.ToNativeX509Certificate2() -IssuerPrivateKey $DpsRootCertificate.PrivateKey.ToNativeRsaKey() -IotHubFqdn $IotHubFqdn

            $TestEnvInfo.Dps.Enrollments.GroupX509 += $X509EnrollmentGroupInfo

            for ($i = 0; $i -lt $DpsX509GroupEnrollmentDevices; $i++) {
                $X509EnrollmentGroupInfo.AddIdentity("group-prov-x509-$i", $DefaultCertificateExpiration) | Out-Null
            }
        }
    }

    # File Upload
    if ($EnableFileUpload -eq $true) {
        Write-Host "Creating Azure Storage account ($StorageAccountName)"
        az storage account create --name "$StorageAccountName" --resource-group "$ResourceGroup" --location "$AzureLocation" --sku Standard_LRS --kind StorageV2 | Out-Null
        Stop-OnError -Step "Creating Azure Storage account"

        $AzureStorageContainerName = "iothubuploads"

        Write-Host "Creating Azure Storage container ($AzureStorageContainerName on $StorageAccountName)"
        az storage container create --name $AzureStorageContainerName --account-name "$StorageAccountName" --only-show-errors | Out-Null
        Stop-OnError -Step "Creating Azure Storage container"

        Write-Host "Getting Azure Storage account connection string"
        $AzureStorageConnectionString=$(az storage account show-connection-string --name "$StorageAccountName" --resource-group "$ResourceGroup" --query connectionString -o tsv)
        Stop-OnError -Step "Getting Azure Storage account connection string"

        if ($EnableCertificateManagement -eq $true) {
            Write-Host "Updating Azure IoT Hub file upload settings (certificate management)"
            az iot hub update --name "$IotHubName" --resource-group "$ResourceGroup" --fcs "$AzureStorageConnectionString" --fc $AzureStorageContainerName --fileupload-sas-ttl 1 --ns-identity-id "$($AzureAdrNamespace.identity.principalId)" | Out-Null
            Stop-OnError -Step "Updating Azure IoT Hub file upload settings (certificate management)"
        } else {    
            Write-Host "Updating Azure IoT Hub file upload settings"
            az iot hub update --name "$IotHubName" --resource-group "$ResourceGroup" --fcs "$AzureStorageConnectionString" --fc $AzureStorageContainerName --fileupload-sas-ttl 1 | Out-Null
            Stop-OnError -Step "Updating Azure IoT Hub file upload settings"
        }
    }

    # Gathering Test Environment settings.
    $TestEnvInfo.AzureResourceGroup = $ResourceGroup
    $TestEnvInfo.Dps.ResourceGroup = $ResourceGroup
    $TestEnvInfo.AzureAdrPolicyName = $AzureAdrPolicyName

    Write-Host "Getting IoT Hub Connection String"
    $TestEnvInfo.IotHub.ConnectionString = $(az iot hub connection-string show -g $ResourceGroup -n $IotHubName --kt primary --pn iothubowner --query connectionString -o tsv)
    Stop-OnError -Step "Get IoT Hub Connection String"

    Write-Host "Getting IoT Hub's Event Hub Connection String"
    $TestEnvInfo.IotHub.EventHub.ConnectionString = $(az iot hub connection-string show -g $ResourceGroup -n $IotHubName --kt primary --pn iothubowner --eh --query connectionString -o tsv)
    Stop-OnError -Step "Get IoT Hub's Event Hub Connection String"

    $TestEnvInfo.IotHub.EventHub.CompatibleName = $AzureIoTHub.properties.eventHubEndpoints.events.path
    $TestEnvInfo.IotHub.EventHub.PartitionCount = $AzureIoTHub.properties.eventHubEndpoints.events.partitionCount

    Write-Host "Getting IoT Hub's Event Hub Consumer Groups"
    az iot hub consumer-group list --hub-name $IotHubName --resource-group $ResourceGroup | ConvertFrom-Json | %{ $TestEnvInfo.IotHub.EventHub.ConsumerGroups += $_.name }
    Stop-OnError -Step "Get IoT Hub's Event Hub Consumer Groups"

    if ($NoDps -eq $false) {
        $TestEnvInfo.Dps.DeviceFqdn = $AzureDps.properties.deviceProvisioningHostName
        $TestEnvInfo.Dps.ServiceFqdn = $AzureDps.properties.serviceOperationsHostName
        $TestEnvInfo.Dps.IdScope = $AzureDps.properties.idScope

        Write-Host "Getting DPS Connection String"
        $TestEnvInfo.Dps.ConnectionString = $(az iot dps connection-string show -g $ResourceGroup -n $DpsName --kt primary --pn provisioningserviceowner --query connectionString -o tsv)
        Stop-OnError -Step "Get DPS Connection String"

        $TestEnvInfo.Dps.RootCaCertificates += $DpsRootCertificate
    }

    return $TestEnvInfo
}

function Get-AzIotTestEnvironment {
    <#
    .SYNOPSIS
    Creates a new set of Azure Resources for testing Azure IoT scenarios, including an IoT Hub and optionally a Device Provisioning Service, with different types of enrollments and devices.

    .DESCRIPTION
    Creates a new set of Azure Resources for testing Azure IoT scenarios, including an IoT Hub and optionally a Device Provisioning Service, with different types of enrollments and devices.

    .PARAMETER AzureSubscriptionId
    Specifies the Azure subscription ID. If not provided, the current Azure CLI session default subscription will be used.
    .PARAMETER ResourceGroup
    Specifies the name of the resource group. If not provided, a new resource group will be created.
    .PARAMETER DpsName
    Specifies the name of the Device Provisioning Service. If not provided, get the one instance in the resource group.
    .PARAMETER IotHubName
    Specifies the name of the IoT Hub. If not provided, get the IoT Hub linked to the DPS or with the specified name.

    .OUTPUTS
    A custom object containing information about the created Azure resources and devices, including connection strings, certificate paths, and enrollment details.

    .EXAMPLE
    PS> $TestEnvInfo = Get-AzIotTestEnvironment -ResourceGroup "myResourceGroupName"
    #>
    param(
        [string]$AzureSubscriptionId = $null,
        [string]$ResourceGroup = $null,
        [string]$DpsName       = $null,
        [string]$IotHubName    = $null
    )

    $TestEnvInfo = [TestEnvironmentInfo]::new()

    # Login to Azure if not already
    $null = & az account show 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Not logged in to Azure. Running 'az login'..."
        & az login --use-device-code
        if ($LASTEXITCODE -ne 0) {
            throw "Azure login failed."
        }
    }
    else {
        Write-Host "Already logged in to Azure."
    }

    $AzureAccount = az account show | ConvertFrom-Json

    # Subscription id...
    if ([string]::IsNullOrWhiteSpace($AzureSubscriptionId)) {
        Stop-OnError -Step "Get Azure account information"
        $AzureSubscriptionId = $AzureAccount.id
    } else {
        $discard = az account set --subscription "$AzureSubscriptionId" --only-show-errors 2>$null
        Stop-OnError -Step "Set Azure subscription"
    }

    # Add Azure IoT extension if not already added (required for some az iot commands, e.g. az iot adr ns create)
    # TODO: install non-preview version after GA.
    $AzCliAzureIotExtension = $(az extension list | Convertfrom-json | ?{$_.name -eq "azure-iot"})

    # ADR credential-policy support requires preview version >= 0.30.0b2.
    # Remove any existing version (stable or older preview) and install the required preview.
    $RequiredVersion = "0.30.0b2"
    if ($AzCliAzureIotExtension -ne $null -and $AzCliAzureIotExtension.version -ne $RequiredVersion) {
        Write-Host "Azure IoT extension found (version $($AzCliAzureIotExtension.version)), upgrading to $RequiredVersion..."
        az extension remove --name azure-iot --only-show-errors | Out-Null
        Stop-OnError -Step "Remove old Azure IoT extension"
        $AzCliAzureIotExtension = $null
    }

    if ($AzCliAzureIotExtension -eq $null) {
        Write-Host "Installing Azure IoT extension (version $RequiredVersion)."
        az extension add --name azure-iot --version $RequiredVersion --allow-preview --only-show-errors | Out-Null
        Stop-OnError -Step "Install Azure IoT extension"
    }

    $AzureResourceGroup = az group show --name "$ResourceGroup" | ConvertFrom-Json

    if ([string]::IsNullOrWhiteSpace($DpsName)) {
        $AzureDpsInstances = az iot dps list --resource-group "$ResourceGroup" | ConvertFrom-Json

        if ($AzureDpsInstances.Count -ne 1) {
            throw "Multiple Azure Device Provisioning services found under resource group $ResourceGroup. Provide DpsName argument to select."
        }

        $AzureDps = $AzureDpsInstances[0]
    } else {
        $AzureDps = az iot dps show --resource-group "$ResourceGroup" --name "$DpsName" | ConvertFrom-Json
    }

    if ([string]::IsNullOrWhiteSpace($IotHubName)) {
        if ($AzureDps.properties.iotHubs.Count -eq 0) {
            throw "Device Provisioning Service ($DpsName) does not have linked IoT hubs"
        }

        $IotHubName = $($AzureDps.properties.iotHubs[0].name.Split('.')[0])
    } else {
        $DpsLinkedIotHub = $AzureDps.properties.iotHubs | ?{$_.name -imatch "$IotHubName" }

        if ($DpsLinkedIotHub -eq $null) {
            throw "Iot Hub $IotHubName is not linked to $DpsName"
        }
    }

    $AzureIoTHub = az iot hub show --resource-group "$ResourceGroup" --name "$IotHubName" | ConvertFrom-Json

    # Gathering Test Environment settings.
    $TestEnvInfo.AzureResourceGroup = $AzureResourceGroup.name
    $TestEnvInfo.Dps.ResourceGroup = $AzureResourceGroup.name

    $AzureAdrNamespaceName = $AzureIotHub.properties.deviceRegistry.namespaceResourceId.split("/")[8]
    $AzureAdrPolicy = az iot adr ns policy list --resource-group "$ResourceGroup" --ns "$AzureAdrNamespaceName" | ConvertFrom-Json

    $TestEnvInfo.AzureAdrPolicyName = $AzureAdrPolicy.name

    Write-Host "Getting IoT Hub Connection String"
    $TestEnvInfo.IotHub.ConnectionString = $(az iot hub connection-string show -g $ResourceGroup -n $IotHubName --kt primary --pn iothubowner --query connectionString -o tsv)
    Stop-OnError -Step "Get IoT Hub Connection String"

    Write-Host "Getting IoT Hub's Event Hub Connection String"
    $TestEnvInfo.IotHub.EventHub.ConnectionString = $(az iot hub connection-string show -g $ResourceGroup -n $IotHubName --kt primary --pn iothubowner --eh --query connectionString -o tsv)
    Stop-OnError -Step "Get IoT Hub's Event Hub Connection String"

    $TestEnvInfo.IotHub.EventHub.CompatibleName = $AzureIoTHub.properties.eventHubEndpoints.events.path
    $TestEnvInfo.IotHub.EventHub.PartitionCount = $AzureIoTHub.properties.eventHubEndpoints.events.partitionCount

    Write-Host "Getting IoT Hub's Event Hub Consumer Groups"
    az iot hub consumer-group list --hub-name $IotHubName --resource-group $ResourceGroup | ConvertFrom-Json | %{ $TestEnvInfo.IotHub.EventHub.ConsumerGroups += $_.name }
    Stop-OnError -Step "Get IoT Hub's Event Hub Consumer Groups"

    $TestEnvInfo.Dps.DeviceFqdn = $AzureDps.properties.deviceProvisioningHostName
    $TestEnvInfo.Dps.ServiceFqdn = $AzureDps.properties.serviceOperationsHostName
    $TestEnvInfo.Dps.IdScope = $AzureDps.properties.idScope

    Write-Host "Getting DPS Connection String"
    $TestEnvInfo.Dps.ConnectionString = $(az iot dps connection-string show -g $ResourceGroup -n $AzureDps.name --kt primary --pn provisioningserviceowner --query connectionString -o tsv)
    Stop-OnError -Step "Get DPS Connection String"

    return $TestEnvInfo    
}

function New-AzIotDotNetSDKE2ETestConfig {
    param(
        [TestEnvironmentInfo]$TestEnvInfo = $null,
        [string] $TestCertificateOutputLocation
    )

    ########################################################################################################
    # Generate self-signed certs and to use in DPS and IoT hub.
    # New certs will be generated each time you run the script as the script cleans up in the end.
    ########################################################################################################

    $subjectPrefix = "IoT Test";
    $rootCommonName = "$subjectPrefix Root CA";
    $intermediateCert1CommonName = "$subjectPrefix Intermediate 1 CA";
    $intermediateCert2CommonName = "$subjectPrefix Intermediate 2 CA";

    $folderPath = "C:\MyNewFolder"

    if (-not (Test-Path -Path $TestCertificateOutputLocation)) 
    {
        New-Item -Path $TestCertificateOutputLocation -ItemType Directory
    }

    $rootCertPath = "$TestCertificateOutputLocation/Root.cer";
    $intermediateCert1CertPath = "$TestCertificateOutputLocation/intermediateCert1.cer";
    $intermediateCert2CertPath = "$TestCertificateOutputLocation/intermediateCert2.cer";
    $intermediateCert2PfxPath = "$TestCertificateOutputLocation/intermediateCert2.pfx"
    $verificationCertPath = "$TestCertificateOutputLocation/verification.cer";

    $iotHubX509DeviceCertCommonName = "Save_iothubx509device1";
    $iotHubX509DevicePfxPath = "$TestCertificateOutputLocation/IotHubX509Device.pfx";
    $iotHubX509CertChainDeviceCommonName = "Save_iothubx509chaindevice1";
    $iotHubX509ChainDevicPfxPath = "$TestCertificateOutputLocation/IotHubX509ChainDevice.pfx";

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
            "-Signer"              = $rootCACert;
        }
        $verificationCert = New-SelfSignedCertificate @verificationCertArgs
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

    # Environment variables for IoT Hub E2E tests
    Write-Host "##vso[task.setvariable variable=IOTHUB_CONNECTION_STRING;isOutput=true]$TestEnvInfo.IotHub.ConnectionString"
    Write-Host "##vso[task.setvariable variable=IOTHUB_X509_DEVICE_PFX_CERTIFICATE;isOutput=true]$iothubX509DevicePfxBase64"
    Write-Host "##vso[task.setvariable variable=IOTHUB_X509_CHAIN_DEVICE_NAME;isOutput=true]$iotHubX509CertChainDeviceCommonName";
    Write-Host "##vso[task.setvariable variable=IOTHUB_X509_CHAIN_DEVICE_PFX_CERTIFICATE;isOutput=true]$iothubX509ChainDevicePfxBase64"

    # Environment variables for DPS E2E tests
    Write-Host "##vso[task.setvariable variable=DPS_IDSCOPE;isOutput=true]$TestEnvInfo.Dps.IdScope"
    Write-Host "##vso[task.setvariable variable=PROVISIONING_CONNECTION_STRING;isOutput=true]$TestEnvInfo.Dps.ConnectionString"
    Write-Host "##vso[task.setvariable variable=DPS_GLOBALDEVICEENDPOINT;isOutput=true]$TestEnvInfo.Dps.DeviceFqdn"
    Write-Host "##vso[task.setvariable variable=DPS_X509_PFX_CERTIFICATE_PASSWORD;isOutput=true]$GroupCertificatePassword"
    Write-Host "##vso[task.setvariable variable=DPS_X509_GROUP_ENROLLMENT_NAME;isOutput=true]$groupEnrollmentId"

    # Environment variables for Azure resources used for E2E tests (common)
    Write-Host "##vso[task.setvariable variable=X509_CHAIN_ROOT_CA_CERTIFICATE;isOutput=true]$x509ChainRootCACertBase64"
    Write-Host "##vso[task.setvariable variable=X509_CHAIN_INTERMEDIATE1_CERTIFICATE;isOutput=true]$x509ChainIntermediate1CertBase64"
    Write-Host "##vso[task.setvariable variable=X509_CHAIN_INTERMEDIATE2_CERTIFICATE;isOutput=true]$x509ChainIntermediate2CertBase64"
    Write-Host "##vso[task.setvariable variable=X509_CHAIN_INTERMEDIATE2_PFX_CERTIFICATE;isOutput=true]$x509ChainIntermediate2PfxBase64"
}

Export-ModuleMember -Function New-AzureResourceGroupName
Export-ModuleMember -Function New-AzIotTestEnvironment
Export-ModuleMember -Function Get-AzIotTestEnvironment
Export-ModuleMember -Function New-AzIotDotNetSDKE2ETestConfig