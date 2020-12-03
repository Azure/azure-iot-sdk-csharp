# Provisioning Device Client Sample - Microsoft Azure IoT SDK for .NET

## Overview

This folder contains samples demonstrating the steps required to dynamically associate devices with IoT hubs using the Microsoft Azure IoT Device Provisioning Service.

To ensure that only authorized devices can be provisioned, two device attestation mechanisms are supported by the service: one based on X.509 certificates and another based on Trusted Platform Module (TPM) devices. In both cases, public/private key authentication will be performed during provisioning.

## Device provisioning in a nutshell

Provisioning is achieved by using a single call to the `ProvisioningDeviceClient.RegisterAsync()` API specifying the `GlobalDeviceEndpoint`, the IDScope (unique for each Provisioning Service deployment), a `SecurityProvider` and a `ProvisioningTransportHandler`:

```C#
ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(globalDeviceEndpoint, s_idScope, security, transport);
DeviceRegistrationResult result = await provClient.RegisterAsync();
if (result.Status != ProvisioningRegistrationStatusType.Assigned) 
{
    Console.WriteLine($"ProvisioningClient AssignedHub: {result.AssignedHub}; DeviceId: {result.DeviceId}");
}
```

To change the transport between AMQP, HTTP and MQTT, check the sample parameters (`-h`).

### Provisioning devices using X.509 certificate-based attestation

Devices must have access to a single certificate with a private key. The private key can be hidden from the application using PKCS #11 Hardware Security Modules.

When Group Enrollment is used, both the _RegistrationID_ as well as the _DeviceId_ will be equal to the Common Name portion of the certificate Subject. (e.g. If the subject is `CN=mydevice O=Contoso C=US`, the RegistrationID and DeviceId will be `mydevice`.) The name must respect the [DeviceId naming constraints](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-identity-registry).

X.509 attestation comes in two flavors: 

1. Group Enrollment
In this case, a single (Intermediate) Certificate Authority certificate is uploaded to the Provisioning Service. Devices have access to certificates issued by this CA. All devices will be using the same policies during IoT Hub provisioning.

The service requires proof of possession of the uploaded CA certificate private key. This can be achieved using the [Proof of possession for X.509 Group Enrollment](../../service/samples/GroupCertificateVerificationSample) tool.

Because Intermediate Authorities may have been issued by the uploaded CA, the application must present the full chain of certificates from the one used during authentication to the one uploaded to the service. E.g.: If `My CA Certificate` was uploaded to the service and `MyDevice1` is the device certificate, the entire chain must be available on the device: 
`<"My CA Certificate", "My Intermediate 1", ..., "My Intermediate N", "MyDevice1>`.

2. Individual Enrollment
In this case, each device certificate gets its own certificate uploaded to the service.
_Note:_ the device certificate can also be a signing certificate of the actual certificate used to authenticate with the service during provisioning.

An example of specifying the authentication X509Certificate using a PKCS12 PFX password-protected file:

```C#
using var certificate = new X509Certificate2(s_certificateFileName, certificatePassword);
using var security = new SecurityProviderX509Certificate(certificate);
// ... (see sample for details)
```

The SDK provides an extension model [SecurityProviderX509](https://github.com/Azure/azure-iot-sdk-csharp/blob/master/shared/src/SecurityProviderX509.cs) that allows hardware vendors to implement custom Hardware Security Modules that store the device certificates. On Windows, PKCS11 HSM devices are supported through the [Certificate Store](https://docs.microsoft.com/windows-hardware/drivers/install/certificate-stores).

An example of implementation for this extension module is the [SecurityProviderX509Certificate](https://github.com/Azure/azure-iot-sdk-csharp/blob/master/shared/src/SecurityProviderX509Certificate.cs) class.

### Provisioning devices using TPM-based attestation

In the case of TPM attestation, a RegistrationID must be supplied by the application.

The TPM attestation supports only Individual Enrollments. The [Endorsement Key](https://technet.microsoft.com/library/cc770443(v=ws.11).aspx) (EK) must be supplied to the service. During provisioning, the [Storage Root Key](https://technet.microsoft.com/library/cc753560(v=ws.11).aspx) (SRK) will also be used to ensure that TPM ownership changes result in devices provisioned to the correct owner.

```C#
using var security = new SecurityProviderTpmSimulator(RegistrationId);
// ... (see sample for details)
```

The SDK provides an extension model [SecurityProviderTpm](https://github.com/Azure/azure-iot-sdk-csharp/blob/master/shared/src/SecurityProviderTpm.cs) that allows hardware vendors to implement custom TPM v2.0 Hardware Security Modules.

The samples use a TPMv2.0 simulator that uses a loopback TCP connection for communication. This is provided for demonstration purposes only and does not provide any security.

## List of samples

- [Provisioning Devices with X.509 certificate attestation](X509Sample)
- [Provisioning Devices with TPM attestation](TpmSample)

### How to run the samples

1. Prepare your development environment. Follow the instructions at ./doc/devbox_setup.md
1. Setup your IoT Hub Device Provisioning Service and associated IoT Hub. Follow the instructions at <https://docs.microsoft.com/azure/iot-dps/quick-setup-auto-provision>.
1. Continue following specific instructions in each of the sample folders.

### Read more

- [Azure IoT Hub Device Provisioning Service (preview) Documentation](https://docs.microsoft.com/azure/iot-dps/)

- [How to use the Azure IoT SDKs for .NET](https://github.com/azure/azure-iot-sdk-csharp#how-to-use-the-azure-iot-sdks-for-net)
