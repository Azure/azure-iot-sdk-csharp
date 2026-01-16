# Certificate Signing Request Sample

This sample demonstrates how to use certificate-based authentication with IoT Hub and the credential management API for certificate renewal.

## Overview

The sample performs the following steps:

1. **Load or Provision Credentials**: Attempts to load existing credentials from disk, or provisions a new device with Azure Device Provisioning Service (DPS) using symmetric key authentication combined with a Certificate Signing Request (CSR).

2. **Connect to IoT Hub**: Connects to the assigned IoT Hub using the X.509 certificate issued by DPS.

3. **Create CSR for Renewal**: Generates a new Certificate Signing Request using the same private key.

4. **Request Certificate Renewal**: Sends the CSR to IoT Hub via the MQTT-based credential management API and receives a renewed certificate.

5. **Reconnect with Renewed Certificate**: Disconnects and reconnects to IoT Hub using the newly issued certificate.

6. **Send Telemetry**: Sends a configurable number of telemetry messages to verify the connection works with the renewed certificate.

## Prerequisites

- .NET 8.0 SDK or later
- An Azure IoT Hub instance
- An Azure Device Provisioning Service (DPS) instance linked to the IoT Hub
- A DPS enrollment group configured for symmetric key attestation with certificate issuance enabled

## Parameters

| Parameter | Short | Required | Default | Description |
|-----------|-------|----------|---------|-------------|
| `--outputDir` | `-o` | Yes | - | Directory to save certificate and key files |
| `--idScope` | `-i` | Yes | - | The ID Scope of the DPS instance |
| `--sasKey` | `-k` | Yes | - | The DPS SAS key (enrollment group primary key) |
| `--deviceName` | `-d` | No | `test-device` | The device registration ID / device name |
| `--provisioningHost` | `-p` | No | `global.azure-devices-provisioning.net` | The DPS global provisioning host |
| `--messageCount` | `-m` | No | `3` | Number of telemetry messages to send after renewal |
| `--transportType` | `-t` | No | `Mqtt_Tcp_Only` | Transport type (Mqtt_Tcp_Only required for certificate renewal) |

## Example Usage

```powershell
# Run the sample
dotnet run -- --outputDir ./certs --idScope 0ne00ABCDEF --sasKey <your-enrollment-group-key>

# Run with custom device name
dotnet run -- --outputDir ./certs --idScope 0ne00ABCDEF --sasKey <key> --deviceName my-device

# Send more telemetry messages
dotnet run -- --outputDir ./certs --idScope 0ne00ABCDEF --sasKey <key> --messageCount 10
```

## Files Generated

The sample saves the following files to the output directory:

- `{deviceName}.cert.pem` - The certificate issued by DPS
- `{deviceName}.key.pem` - The EC private key (SECP256R1/prime256v1)
- `{deviceName}.json` - Metadata containing the assigned hub and device ID
- `{deviceName}_renewed.cert.pem` - The renewed certificate from IoT Hub

## Certificate Renewal Flow

The certificate renewal uses the IoT Hub credential management API, which is accessed via MQTT:

1. The device generates a new CSR using the existing private key
2. The CSR is sent to IoT Hub using `DeviceClient.SendCertificateSigningRequestAsync()`
3. IoT Hub validates the request and issues a new certificate
4. The device receives the certificate chain in the response
5. The device can then reconnect using the new certificate

This allows devices to renew their certificates without re-provisioning through DPS.

## Security Considerations

- Private keys are stored unencrypted on disk. In production, consider using secure key storage.
- The sample uses SECP256R1 (prime256v1) elliptic curve keys, which are widely supported.
- Certificate files should be protected with appropriate file system permissions.
