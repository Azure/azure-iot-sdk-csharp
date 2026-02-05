# Certificate Signing Request Sample

This sample demonstrates how to use certificate-based authentication with IoT Hub and the credential management API for certificate renewal.

## Overview

The sample performs the following steps:

1. **Load Existing Credentials**: Loads pre-existing credentials from disk (certificate, private key, and metadata file containing the IoT Hub hostname and device ID).

2. **Connect to IoT Hub**: Connects to IoT Hub using the existing X.509 certificate.

3. **Verify Connectivity**: Sends a test message to verify the connection works.

4. **Create CSR for Renewal**: Generates a new Certificate Signing Request using the existing private key.

5. **Request Certificate Renewal**: Sends the CSR to IoT Hub via the MQTT-based credential management API and receives a renewed certificate.

6. **Reconnect with Renewed Certificate**: Disconnects and reconnects to IoT Hub using the newly issued certificate.

7. **Send Telemetry**: Sends a configurable number of telemetry messages to verify the connection works with the renewed certificate.

> **Note**: This sample does not perform initial device provisioning. It requires that the device has already been provisioned and has valid credentials. For initial provisioning with CSR, use the Azure Python SDK or Azure CLI.

## Prerequisites

- .NET 8.0 SDK or later
- An Azure IoT Hub instance
- A device already provisioned in IoT Hub with X.509 certificate authentication
- Pre-existing credential files (see [Required Files](#required-files) below)

## Parameters

| Parameter | Short | Required | Default | Description |
|-----------|-------|----------|---------|-------------|
| `--outputDir` | `-o` | Yes | - | Directory containing certificate, key, and metadata files |
| `--deviceName` | `-d` | No | `test-device` | The device name (used to locate credential files) |
| `--messageCount` | `-m` | No | `3` | Number of telemetry messages to send after renewal |
| `--transportType` | `-t` | No | `Mqtt_Tcp_Only` | Transport type (Mqtt_Tcp_Only required for certificate renewal) |

## Required Files

The sample expects the following files in the output directory:

| File | Description |
|------|-------------|
| `{deviceName}.cert.pem` | The X.509 certificate in PEM format |
| `{deviceName}.key.pem` | The EC private key in PEM format (SECP256R1/prime256v1) |
| `{deviceName}.json` | Metadata file (see format below) |

The metadata JSON file must contain:

```json
{
  "assigned_hub": "your-hub.azure-devices.net",
  "device_id": "your-device-id"
}
```

## Example Usage

```powershell
# Run the sample with existing credentials
dotnet run -- --outputDir ./certs

# Run with a custom device name
dotnet run -- --outputDir ./certs --deviceName my-device

# Send more telemetry messages after renewal
dotnet run -- --outputDir ./certs --messageCount 10
```

## Files Generated

The sample generates the following file after successful certificate renewal:

- `{deviceName}_renewed.cert.pem` - The renewed certificate from IoT Hub

## Certificate Renewal Flow

The certificate renewal uses the IoT Hub credential management API, which is accessed via MQTT:

1. The device connects to IoT Hub using its existing certificate
2. The device generates a new CSR using the existing private key
3. The CSR is sent to IoT Hub using `DeviceClient.SendCertificateSigningRequestAsync()`
4. IoT Hub validates the request and issues a new certificate
5. The device receives the certificate chain in the response
6. The device disconnects and reconnects using the new certificate

This allows devices to renew their certificates without re-provisioning.

## Security Considerations

- Private keys are stored unencrypted on disk. In production, consider using secure key storage (e.g., HSM, TPM, or secure enclave).
- The sample uses SECP256R1 (prime256v1) elliptic curve keys, which are widely supported.
- Certificate files should be protected with appropriate file system permissions.
