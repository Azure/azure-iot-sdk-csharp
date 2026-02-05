# Certificate Signing Request Sample

This sample demonstrates certificate-based authentication with IoT Hub, including initial provisioning via DPS with CSR and certificate renewal via the IoT Hub credential management API.

## Overview

The sample supports two modes of operation:

1. **Initial Provisioning via DPS**: When credentials don't exist, provisions the device via DPS with a CSR to obtain an issued certificate
2. **Certificate Renewal**: When credentials exist, connects to IoT Hub and renews the certificate using the credential management API

## Prerequisites

1. **Azure IoT Hub** - An Azure IoT Hub instance
2. **Azure Device Provisioning Service (DPS)** - Linked to your IoT Hub (for initial provisioning)
3. **DPS Enrollment** - Configured for certificate issuance
4. **.NET 8.0 SDK** or later

## Configuration

### Initial Provisioning (Individual Enrollment)

```bash
dotnet run -- \
    --outputDir "./certs" \
    --deviceName "my-device" \
    --idScope "<your-dps-id-scope>" \
    --symmetricKey "<your-symmetric-key>"
```

### Initial Provisioning (Group Enrollment)

For group enrollments, use `--enrollmentGroupKey` and the sample will automatically derive the device-specific key:

```bash
dotnet run -- \
    --outputDir "./certs" \
    --deviceName "my-device" \
    --idScope "<your-dps-id-scope>" \
    --enrollmentGroupKey "<your-enrollment-group-primary-key>"
```

### Certificate Renewal (Existing Credentials)

```bash
dotnet run -- \
    --outputDir "./certs" \
    --deviceName "my-device"
```

## Command Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--outputDir` | `-o` | Directory for credential files | Required |
| `--deviceName` | `-d` | Device name (also used as DPS registration ID) | `test-device` |
| `--messageCount` | `-m` | Number of telemetry messages after renewal | `3` |
| `--transportType` | `-t` | Transport protocol | `Mqtt_Tcp_Only` |
| `--idScope` | `-i` | DPS ID Scope | Required for DPS |
| `--symmetricKey` | `-k` | Symmetric key for individual enrollment | Required for individual enrollment |
| `--enrollmentGroupKey` | `-e` | Enrollment group key (device key will be derived) | Required for group enrollment |
| `--globalDeviceEndpoint` | `-g` | DPS global endpoint | `global.azure-devices-provisioning.net` |
| `--csrKeyType` | | Key type for CSR (`ECC` or `RSA`) | `ECC` |
| `--rsaKeySize` | | RSA key size in bits | `2048` |

## Credential Files

The sample uses the following files in the output directory:

| File | Description |
|------|-------------|
| `{deviceName}.cert.pem` | X.509 certificate chain (PEM format) |
| `{deviceName}.key.pem` | Private key (PEM format) |
| `{deviceName}.json` | Metadata with `assigned_hub` and `device_id` |
| `{deviceName}_renewed.cert.pem` | Renewed certificate (after renewal) |

## Additional Resources

- [Azure Device Provisioning Service documentation](https://docs.microsoft.com/azure/iot-dps/)
- [X.509 certificate attestation](https://docs.microsoft.com/azure/iot-dps/concepts-x509-attestation)
- [Azure IoT Hub documentation](https://docs.microsoft.com/azure/iot-hub/)
