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

### Using Symmetric Key Authentication

```bash
dotnet run -- \
    --IdScope "<your-dps-id-scope>" \
    --RegistrationId "<your-registration-id>" \
    --SymmetricKey "<your-symmetric-key>" \
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
| `--SymmetricKey` | `-k` | Symmetric key for DPS auth | Required for SymmetricKey |
| `--X509CertPath` | `-c` | Path to X.509 certificate (PFX) | Required for X509 |
| `--X509CertPassword` | `-w` | X.509 certificate password | Optional |
| `--CsrKeyType` | | Key type for CSR (`ECC` or `RSA`) | `ECC` |
| `--RsaKeySize` | | RSA key size in bits | `2048` |
| `--OutputCertPath` | `-o` | Output path for issued certificate | `issued_certificate.pem` |
| `--OutputKeyPath` | | Output path for private key | `private_key.pem` |
| `--SendTelemetry` | `-s` | Send test telemetry after registration | `true` |

## Example Output

```
=== DPS CSR Provisioning Sample ===

Step 1: Generating key pair and Certificate Signing Request (CSR)...
  CSR generated successfully.
  Key type: ECC

  Private key saved to: private_key.pem

Step 2: Creating security provider (SymmetricKey)...
  Registration ID: my-device-001

Step 3: Creating provisioning client...
  Using transport type: Mqtt
  Global endpoint: global.azure-devices-provisioning.net
  ID Scope: 0ne00XXXXXX
  Transport: Mqtt

Step 4: Registering with DPS (including CSR)...
  Registration status: Assigned
  Device ID: my-device-001
  Assigned hub: my-iothub.azure-devices.net

Step 5: Processing issued certificate...
  Received certificate chain with 2 certificate(s).
  Certificate chain saved to: issued_certificate.pem

Step 6: Creating certificate with private key for IoT Hub authentication...
  Certificate subject: CN=my-device-001
  Certificate thumbprint: ABC123...
  Valid from: 2/4/2026 12:00:00 AM
  Valid until: 2/4/2027 12:00:00 AM

Step 7: Connecting to IoT Hub using issued certificate...
  Connected to IoT Hub successfully.

Step 8: Sending test telemetry message...
  Telemetry message sent successfully.

=== Sample completed successfully ===

You can now use the following files to authenticate with IoT Hub:
  Certificate: issued_certificate.pem
  Private key: private_key.pem
```

## Security Considerations

1. **Private Key Protection**: The private key generated for the CSR is sensitive and should be securely stored. In production, consider using hardware security modules (HSM) or secure enclaves.

2. **Certificate CN Validation**: The Common Name (CN) in the CSR should match the registration ID.

3. **Key Algorithm**: This sample supports both ECC (P-256) and RSA keys. ECC is recommended for better performance and smaller key sizes.

4. **Transport Security**: All communication with DPS and IoT Hub is encrypted using TLS.

## Environment Variables (Alternative Configuration)

You can also use environment variables instead of command-line arguments:

| Variable | Description |
|----------|-------------|
| `PROVISIONING_HOST` | DPS global endpoint |
| `PROVISIONING_IDSCOPE` | DPS ID Scope |
| `PROVISIONING_REGISTRATION_ID` | Device registration ID |
| `PROVISIONING_SAS_KEY` | Symmetric key |
| `PROVISIONING_X509_CERT_FILE` | X.509 certificate path |

## Troubleshooting

### "No certificate was issued by DPS"
- Ensure your DPS enrollment is configured for certificate issuance
- Verify that Azure Device Registry integration is properly set up

### "Registration failed"
- Check the error code and message in the output
- Verify your DPS ID Scope and registration credentials
- Ensure the registration ID matches your enrollment configuration

### "Failed to connect to IoT Hub"
- Verify the issued certificate is valid and not expired
- Check that the certificate chain is complete
- Ensure the IoT Hub allows X.509 authentication

## Additional Resources

- [Azure Device Provisioning Service documentation](https://docs.microsoft.com/azure/iot-dps/)
- [X.509 certificate attestation](https://docs.microsoft.com/azure/iot-dps/concepts-x509-attestation)
- [Azure IoT Hub documentation](https://docs.microsoft.com/azure/iot-hub/)
