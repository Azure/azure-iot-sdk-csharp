# DPS Certificate Signing Request (CSR) Provisioning Sample

This sample demonstrates how to provision a device using Azure Device Provisioning Service (DPS) with a Certificate Signing Request (CSR). The device generates a CSR, submits it during the registration process, and receives an issued certificate from DPS that can be used to authenticate with Azure IoT Hub.

## Overview

The CSR provisioning flow works as follows:

1. **Generate Key Pair & CSR**: The device generates a private/public key pair and creates a Certificate Signing Request (CSR)
2. **Register with DPS**: The device authenticates to DPS (using symmetric key or X.509) and includes the CSR in the registration request
3. **Receive Issued Certificate**: DPS returns an issued certificate chain signed by the configured certificate authority
4. **Connect to IoT Hub**: The device uses the issued certificate with its private key to authenticate with the assigned IoT Hub

## Prerequisites

1. **Azure IoT Hub** - An Azure IoT Hub instance
2. **Azure Device Provisioning Service (DPS)** - Linked to your IoT Hub
3. **DPS Enrollment** - Configured for certificate issuance (requires Azure Device Registry integration)
4. **.NET 8.0 SDK** or later

## Configuration

### Using Symmetric Key Authentication (Individual Enrollment)

```bash
dotnet run -- \
    --IdScope "<your-dps-id-scope>" \
    --RegistrationId "<your-registration-id>" \
    --SymmetricKey "<your-symmetric-key>" \
    --AuthType SymmetricKey
```

### Using Symmetric Key Authentication (Group Enrollment)

For group enrollments, use `--EnrollmentGroupKey` and the sample will automatically derive the device-specific key:

```bash
dotnet run -- \
    --IdScope "<your-dps-id-scope>" \
    --RegistrationId "<your-registration-id>" \
    --EnrollmentGroupKey "<your-enrollment-group-primary-key>" \
    --AuthType SymmetricKey
```

### Using X.509 Certificate Authentication

```bash
dotnet run -- \
    --IdScope "<your-dps-id-scope>" \
    --RegistrationId "<your-registration-id>" \
    --X509CertPath "<path-to-pfx-file>" \
    --X509CertPassword "<certificate-password>" \
    --AuthType X509
```

## Command Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--IdScope` | `-i` | DPS ID Scope | Required |
| `--RegistrationId` | `-r` | Device registration ID | Required |
| `--GlobalDeviceEndpoint` | `-g` | DPS global endpoint | `global.azure-devices-provisioning.net` |
| `--TransportType` | `-t` | Transport protocol | `Mqtt` |
| `--AuthType` | `-a` | Authentication type (`SymmetricKey` or `X509`) | `SymmetricKey` |
| `--SymmetricKey` | `-k` | Symmetric key for individual enrollment | Required for SymmetricKey (individual) |
| `--EnrollmentGroupKey` | `-e` | Enrollment group key (device key will be derived) | Required for SymmetricKey (group) |
| `--X509CertPath` | `-c` | Path to X.509 certificate (PFX) | Required for X509 |
| `--X509CertPassword` | `-w` | X.509 certificate password | Optional |
| `--CsrKeyType` | | Key type for CSR (`ECC` or `RSA`) | `ECC` |
| `--RsaKeySize` | | RSA key size in bits | `2048` |
| `--OutputCertPath` | `-o` | Output path for issued certificate | `issued_certificate.pem` |
| `--OutputKeyPath` | | Output path for private key | `private_key.pem` |
| `--SendTelemetry` | `-s` | Send test telemetry after registration | `true` |

## Additional Resources

- [Azure Device Provisioning Service documentation](https://docs.microsoft.com/azure/iot-dps/)
- [X.509 certificate attestation](https://docs.microsoft.com/azure/iot-dps/concepts-x509-attestation)
- [Azure IoT Hub documentation](https://docs.microsoft.com/azure/iot-hub/)
