# Provisioning Device Client Sample - Microsoft Azure IoT SDK for .NET

## Overview

_This documentation is preliminary and subject to change._

This folder contains samples demonstrating the steps required to dynamically associate devices with IoT hubs using the Microsoft Azure IoT Device Provisioning Service.

To ensure that only authorized devices can be provisioned, two device attestation mechanisms are supported by the service: one based on X.509 certificates and another based on Trusted Platform Module (TPM) devices. In both cases, public/private key authentication will be performed during provisioning.

_Preview only:_ The SDK currently supports two communication protocols: HTTP and AMQP over TLS.

## Device provisioning in a nutshell

Provisioning is achieved by using a single call to the `ProvisioningDeviceClient.RegisterAsync()` API specifying the IDScope (unique for each Provisioning Service deployment), a `SecurityClient` and a `ProvisioningTransportHandler`:

```C#
    ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(s_idScope, security, transport);
    DeviceRegistrationResult result = await provClient.RegisterAsync();
    if (result.Status != ProvisioningRegistrationStatusType.Assigned) 
    {
       Console.WriteLine($"ProvisioningClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");
    }
```

### Provisioning devices using X.509 certificate-based attestation

Devices must have access to a single certificate with a private key. The private key can be hidden from the application using PKCS #11 Hardware Security Modules.

When an X.509 certificate is used, both the _RegistrationID_ as well as the _DeviceID_ will be equal to the Common Name portion of the certificate Subject. (e.g. If the subject is `CN=mydevice O=Contoso C=US`, the RegistrationID and DeviceID will be `mydevice`.) The name must respect the [DeviceID naming constraints](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-identity-registry).

X.509 attestation comes in two flavors: 

1. Group Enrollment
In this case, a single (Intermediate) Certificate Authority certificate is uploaded to the Provisioning Service. Devices have access to certificates issued by this CA. All devices will be using the same policies during IoT Hub provisioning.

The service requires proof of possession of the uploaded CA certificate private key. This can be achieved using the [Proof of possession for X.509 Group Enrollment](..\..\service\samples\GroupCertificateVerificationSample) tool.

Because Intermediate Authorities may have been issued by the uploaded CA, the application must present the full chain of certificates from the one used during authentication to the one uploaded to the service. E.g.: If `My CA Certificate` was uploaded to the service and `MyDevice1` is the device certificate, the entire chain must be available on the device: 
`<"My CA Certificate", "My Intermediate 1", ..., "My Intermediate N", "MyDevice1>`.

2. Individual Enrollment
In this case, each device certificate gets its own certificate uploaded to the service.
_Note:_ the device certificate can also be a signing certificate of the actual certificate used to authenticate with the service during provisioning.

An example of specifying the authentication X509Certificate using a PKCS12 PFX password-protected file:

```C#
    using (var certificate = new X509Certificate2(s_certificateFileName, certificatePassword))
    using (var security = new SecurityClientX509(certificate))
    {
        // ... (see sample for details)
    }
```

The SDK provides an extension model [SecurityClientHsmX509](https://github.com/Azure/azure-iot-sdk-csharp/blob/master/shared/Microsoft.Azure.Devices.Shared/SecurityClientHsmX509.cs) that allows hardware vendors to implement custom Hardware Security Modules that store the device certificates. On Windows, PKCS11 HSM devices are supported through the [Certificate Store](https://docs.microsoft.com/en-us/windows-hardware/drivers/install/certificate-stores).

An example of implementation for this extension module is the [SecurityClientX509](https://github.com/Azure/azure-iot-sdk-csharp/blob/master/shared/Microsoft.Azure.Devices.Shared/SecurityClientX509.cs) class.

### Provisioning devices using TPM based attestation

In the case of TPM attestation, a RegistrationID must be supplied by the application.

The TPM attestation supports only Individual Enrollments. The [Endorsement Key](https://technet.microsoft.com/en-us/library/cc770443(v=ws.11).aspx) (EK) must be supplied to the service. During provisioning, the [Storage Root Key](https://technet.microsoft.com/en-us/library/cc753560(v=ws.11).aspx) (SRK) will also be used to ensure that TPM ownership changes result in devices provisioned to the correct owner.


```C#
    using (var security = new SecurityClientTpmSimulator(RegistrationId))
    {
        // ... (see sample for details)
    }
```

The SDK provides an extension model [SecurityClientHsmTpm](https://github.com/Azure/azure-iot-sdk-csharp/blob/master/shared/Microsoft.Azure.Devices.Shared/SecurityClientHsmTpm.cs) that allows hardware vendors to implement custom TPM v2.0 Hardware Security Modules.

The samples use a TPMv2.0 simulator that uses a loopback TCP connection for communication. This is provided for demonstration purposes only and does not provide any security.

## List of samples

- [Provisioning Devices with X.509 certificate attestation](ProvisioningDeviceClientX509)
- [Provisioning Devices with TPM attestation](ProvisioningDeviceClientTpm)

### How to run the samples

_Preview only:_ Running the samples requires building from sources. This will not be necessary once NuGet packages are released.

1. Prepare your development environment. Follow the instructions at https://github.com/Azure/azure-iot-sdk-csharp/blob/master/device/doc/devbox_setup.md

2. Build the environment. In the root of your clone, type:

```build -clean -wip_provisioning```

3. While that's building, setup your IoT Hub Device Provisioning Service (preview) and associated IoT Hub. Follow the instructions at https://docs.microsoft.com/en-us/azure/iot-dps/quick-setup-auto-provision

4. Continue following specific instructions in each of the sample folders.

### Read More

- [Azure IoT Hub Device Provisioning Service (preview) Documentation](https://docs.microsoft.com/en-us/azure/iot-dps/)

- [How to use the Azure IoT SDKs for .NET](https://github.com/azure/azure-iot-sdk-csharp#how-to-use-the-azure-iot-sdks-for-net)
