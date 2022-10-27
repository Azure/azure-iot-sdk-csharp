# Best Practice Solution Sample with X.509 Attestation

## Objective

This doc demonstrates a real-world example to help you get started with building your own custom IoT cloud solution with X.509 certificate-based authentication. See [here](https://learn.microsoft.com/en-us/azure/iot-dps/concepts-service#attestation-mechanism) to review available attestation mechanisms.

## Example

Company-X is a manufacturing company that wants to manage various types of products remotely.

The business needs of Company-X are -

- provision devices securely at scale
- group devices by production line
- collect data from devices
- manage and monitor devices remotely (e.g., controlling the temperature of a thermostat)

## Recommended Solution

![solution](media/auth_flow_diagram.png)
> **Note**\
> The Device Process is the device (host) application that holds the provisioning client and IoT hub communication code.

### Prerequisites

1. Create a resource group and one or more IoT hubs.
2. Set up an IoT hub Device Provisioning Service (DPS) instance.
3. Link the IoT hub(s) to the DPS instance.
See [here](https://learn.microsoft.com/en-us/azure/iot-dps/quick-setup-auto-provision) for the instructions.

In this solution, we will use the standard X.509 CA certificate authentication.

### 1. Determine the Public Key Infrastructure (PKI) design

See [here](https://learn.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2012-R2-and-2012/dn786436(v=ws.11)) for Public Key Infrastructure (PKI) design options.

| PKI Design option  |  Use case |
| ----------- | ------------|
| Implement a completely self-managed PKI within your organization that contains internal Certificate Authorities (CAs) chained to an internal root CA at the top of the chain | For experimentation or use in closed IoT networks
| Purchase a CA certificate from a commercial CA and issue certificates within the organization from internal, self-managed CAs that are chained to the external root CA | For production environments if your devices will interact with third-party products or services |

In this solution, we will use a self-managed PKI. If you want to see a sample that registers a device to your DPS instance using a self-signed device certificate, see [here](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/device/samples/Getting%20Started/X509Sample).

### 2. Generate self-signed X.509 certificates and verify root certificate

1. Create a secure string password to use in the creation of self-signed certificates. Open Windows PowerShell as administrator.

```powershell
    $password= ConvertTo-SecureString <your password> -AsPlainText -Force
```

2. [X509AuthSetup.ps1](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/device/samples/solutions/BestPracticeSampleX509/X509AuthSetup.ps1) creates the root, intermediate, device certificate, and uploads the root certificate to your DPS instance, and performs proof-of-possession.

```powershell
    .\X509AuthSetup.ps1 `
        -certFolderPath <Path where the certificates will be placed> `
        -rootCertPassword $password `
        -dpsResourceGroup <DPS instance resource group> `
        -dpsName <DPS instance name> `
        -deviceId <device Id>
```

[X509AuthSetup.ps1](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/device/samples/solutions/BestPracticeSampleX509/X509AuthSetup.ps1)  first issues a self-signed root CA certificate. Then it uses the root CA certificate to generate a unique intermediate certificate for each product line. Finally, it uses the production line certificate, to generate a unique device (end-entity) certificate for each device manufactured on the line.

> **Note**\
> Read more about X.509 certificate attestation [here](https://learn.microsoft.com/en-us/azure/iot-dps/concepts-x509-attestation).
> To know more about the parameters used in the Export-PfxCertificate command, read [here](https://learn.microsoft.com/en-us/powershell/module/pki/export-pfxcertificate?view=windowsserver2022-ps#-password).
> For more details about the proof-of-possession process, see [here](https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-x509ca-concept#proof-of-possession) and [here](https://learn.microsoft.com/en-us/azure/iot-dps/how-to-verify-certificates).

### 3. Create a DPS enrollment with an intermediate certificate

An enrollment group is a group of devices that share a specific attestation method. The enrollment group supports X.509 certificate attestation. Devices in an X.509 enrollment group present X.509 device certificates that have been signed by the same intermediate CA.

We will use the generated intermediate certificate to group devices by production lines. See [here](https://learn.microsoft.com/en-us/azure/iot-dps/concepts-x509-attestation#why-are-intermediate-certs-useful) to read more about intermediate certificates.

[GenerateGroupEnrollment.ps1](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/device/samples/solutions/BestPracticeSampleX509/GenerateGroupEnrollment.ps1) creates an enrollment group in your DPS instance using the generated intermediate certificate.
You can specify initialTwinState, provisioning status, device capabilities, iothubName, eTag, etc. To learn more about optional parameters, see [here](https://learn.microsoft.com/en-us/cli/azure/iot/dps/enrollment-group?view=azure-cli-latest#az-iot-dps-enrollment-group-create).

```powershell
    .\GenerateGroupEnrollment.ps1 `
        -intermediateCertName <intermediate certificate file name including the path> `
        -resourceGroup <DPS instance resource group> `
        -dpsName <DPS instance name>
```

### 4. Provision a device through DPS and connect to IoT Hub

In this step, we will use the chained device certificate to provision a device to an IoT Hub using the enrollment group. Devices provisioned through the same enrollment group will share the initial configuration and will be assigned to one of the linked IoT Hub(s).

1. Obtain the IDScope of the DPS instance from Azure Portal.
2. To build the sample application using dotnet, from terminal navigate to the sample folder (where the .csproj file lives). Then execute the following command and check for build errors:

```powershell
    dotnet build
```

3. Set the IdScope, device certificate, and certificate password.

```powershell
    dotnet run --s <IdScope> --c <CertificateName> --p <CertificatePassword>
```

4. Exchange messages between the device process and the IoT hub.

> **Note**\
> RegistrationId is set to the subject common name of the device certificate. To read more about RegistrationId, see [here](https://learn.microsoft.com/en-us/azure/iot-dps/concepts-service#registration-id).
> For more details about the sample implementation, see [here](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/iothub/device/samples/how%20to%20guides/DeviceReconnectionSample).

![x509-bootsequence](media/bootsequence.png)

### Optional - Clean-up

cleanup.ps1 script will remove the enrollment group in your DPS instance and the device in the IoT hub.

```powershell
    .\cleanup.ps1 `
        -resourceGroup <resource group> `
        -dpsName <DPS instance name> `
        -iothubName <IoT hub instance name> `
        -deviceId <device Id>
```

## Read More

- [Best practices for large-scale IoT device deployments](https://learn.microsoft.com/en-us/azure/iot-dps/concepts-deploy-at-scale)
- [Security practices for Azure IoT device manufacturers](https://learn.microsoft.com/en-us/azure/iot-dps/concepts-device-oem-security-practices)
- [X.509 certificate attestation](https://learn.microsoft.com/en-us/azure/iot-dps/concepts-x509-attestation)
- [Create an X.509 enrollment group with DPS service SDK](https://learn.microsoft.com/en-us/azure/iot-dps/quick-enroll-device-x509?pivots=programming-language-csharp)
