# Microsoft Azure IoT SDK for .NET

### Contents

This repository contains the following:

- **Microsoft Azure IoT Hub device SDK for C#** to connect client devices to Azure IoT Hub with .NET.
- **Microsoft Azure IoT Hub service SDK for C#** to manage your IoT Hub service instance from a back-end .NET application.
- **Microsoft Azure Provisioning device SDK for C#** to provision devices to Azure IoT Hub with .NET.
- **Microsoft Azure Provisioning service SDK for C#** to manage your Provisioning service instance from a back-end .NET application.

### Build status

Due to security considerations, build logs are not publicly available.

| Service Environment                                                   | Status                                                                                                                                                                                                                                                                                        |
| ---                                                                   | ---                                                                                                                                                                                                                                                                                           |
| [Master](https://github.com/Azure/azure-iot-sdk-csharp/tree/master)   | [![Build Status](https://azure-iot-sdks.visualstudio.com/azure-iot-sdks/_apis/build/status/csharp/CSharp%20Prod%20-%20West%20Central%20US?branchName=master)](https://azure-iot-sdks.visualstudio.com/azure-iot-sdks/_build/latest?definitionId=44&repositoryFilter=9&branchName=master)      |
| [Preview](https://github.com/Azure/azure-iot-sdk-csharp/tree/preview) | [![Build Status](https://azure-iot-sdks.visualstudio.com/azure-iot-sdks/_apis/build/status/csharp/CSharp%20Canary%20-%20Central%20US%20EUAP?branchName=preview)](https://azure-iot-sdks.visualstudio.com/azure-iot-sdks/_build/latest?definitionId=402&repositoryFilter=9&branchName=preview) |

### Recommended NuGet packages

| Package Name                                          | Release Version                                           | Pre-release Version                                           |
| ---                                                   | ---                                                       | ---                                                           |
| Microsoft.Azure.Devices.Client                        | [![NuGet][iothub-device-release]][iothub-device-nuget]    | [![NuGet][iothub-device-prerelease]][iothub-device-nuget]     |
| Microsoft.Azure.Devices                               | [![NuGet][iothub-service-release]][iothub-service-nuget]  | [![NuGet][iothub-service-prerelease]][iothub-service-nuget]   |
| Microsoft.Azure.Devices.Shared                        | [![NuGet][iothub-shared-release]][iothub-shared-nuget]    | [![NuGet][iothub-shared-prerelease]][iothub-shared-nuget]     |
| Microsoft.Azure.Devices.Provisioning.Client           | [![NuGet][dps-device-release]][dps-device-nuget]          | [![NuGet][dps-device-prerelease]][dps-device-nuget]           |
| Microsoft.Azure.Devices.Provisioning.Transport.Amqp   | [![NuGet][dps-device-amqp-release]][dps-device-amqp-nuget]| [![NuGet][dps-device-amqp-prerelease]][dps-device-amqp-nuget] |
| Microsoft.Azure.Devices.Provisioning.Transport.Http   | [![NuGet][dps-device-http-release]][dps-device-http-nuget]| [![NuGet][dps-device-http-prerelease]][dps-device-http-nuget] |
| Microsoft.Azure.Devices.Provisioning.Transport.Mqtt   | [![NuGet][dps-device-mqtt-release]][dps-device-mqtt-nuget]| [![NuGet][dps-device-mqtt-prerelease]][dps-device-mqtt-nuget] |
| Microsoft.Azure.Devices.Provisioning.Service          | [![NuGet][dps-service-release]][dps-service-nuget]        | [![NuGet][dps-service-prerelease]][dps-service-nuget]         |
| Microsoft.Azure.Devices.Provisioning.Security.Tpm     | [![NuGet][dps-tpm-release]][dps-tpm-nuget]                | [![NuGet][dps-tpm-prerelease]][dps-tpm-nuget]                 |
| Microsoft.Azure.Devices.DigitalTwin.Client            | N/A                                                       | [![NuGet][pnp-device-prerelease]][pnp-device-nuget]           |
| Microsoft.Azure.Devices.DigitalTwin.Service           | N/A                                                       | [![NuGet][pnp-service-prerelease]][pnp-service-nuget]         |

The API reference documentation for .NET SDK is [here][dotnet-api-reference].

To find SDKs in other languages for Azure IoT, please refer to the [azure-iot-sdks][azure-iot-sdks] repository.
For IoT Hub Management SDK in .NET, please visit [azure-sdk-for-net](https://github.com/Azure/azure-sdk-for-net) repository

## Need support?

- Have a feature request for SDKs? Please post it on [User Voice](https://feedback.azure.com/forums/321918-azure-iot) to help us prioritize.
- Have a technical question? Ask on [Stack Overflow](https://stackoverflow.com/questions/tagged/azure-iot-hub) with tag “azure-iot-hub”.
- Need Support? Every customer with an active Azure subscription has access to [support](https://docs.microsoft.com/azure/azure-supportability/how-to-create-azure-support-request) with guaranteed response time. Consider submitting a ticket and get assistance from Microsoft support team.
- Found a bug? Please help us fix it by thoroughly documenting it and filing an issue on GitHub (C, Java, .NET, Node.js, Python).

## Developing applications for Azure IoT

Visit [Azure IoT Dev Center][iot-dev-center] to learn more about developing applications for Azure IoT.

## Samples

Most of our samples are available at [Azure IoT Samples for C#](https://github.com/Azure-Samples/azure-iot-samples-csharp).

If you are looking for a good device sample to get started with, please see the [device reconnection sample](https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/DeviceReconnectionSample).
It shows how to connect a device, handle disconnect events, cases to handle when making calls, and when to re-initialize the `DeviceClient`.

## Contribute to the Azure IoT C# SDK

If you would like to build or change the SDK source code, please follow the [devguide](doc/devguide.md).

## OS platforms and hardware compatibility

> .NET Standard 1.3 (IoT Hub SDKs only) is last supported in the [2020-02-27](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/2020-2-27) and in the [2020-1-31 LTS](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-1-31) releases.

The IoT Hub device SDK for .NET can be used with a broad range of device platforms and is officially supported on the following Operating Systems:

*  Windows versions officially supported by Microsoft.
*  [Linux distributions](https://docs.microsoft.com/en-us/dotnet/core/install/linux) supported by .NET core.

> Note: For Linux, we test our clients against Ubuntu 16.04.7 LTS.

The NuGet packages provide support for the following .NET flavors:
- .NET Standard 2.1
- .NET Standard 2.0
- .NET Framework 4.7.2 (IoT Hub SDKs only)
- .NET Framework 4.5.1 (IoT Hub SDKs only)

For details on .NET support see the [.NET Standard documentation](https://docs.microsoft.com/dotnet/standard/net-standard).
For details on OS support see the following resources:

- [.NET Core Runtime ID Catalog](https://docs.microsoft.com/dotnet/core/rid-catalog)
- [.NET Framework System Requirements](https://docs.microsoft.com/dotnet/framework/get-started/system-requirements)
- [Configure TLS Protocol Version and Ciphers](./configure_tls_protocol_version_and_ciphers.md)

## Key features and roadmap

### IoT Hub Device SDK

:heavy_check_mark: feature available  :heavy_multiplication_x: feature planned but not supported  :heavy_minus_sign: no support planned

| Features                                                                                                         | mqtt                | mqtt-ws             | amqp                | amqp-ws             | https               | Description                                                                                                                                                                                                                                                            |
|------------------------------------------------------------------------------------------------------------------|---------------------|---------------------|---------------------|---------------------|---------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [Authentication](https://docs.microsoft.com/azure/iot-hub/iot-hub-security-deployment)                     | :heavy_check_mark:  | :heavy_check_mark:* | :heavy_check_mark:  | :heavy_check_mark:* | :heavy_check_mark:* | Connect your device to IoT Hub securely with supported authentication, including private key, SASToken, X-509 Self Signed and X-509 CA Signed. </br> *IoT Hub only supports X-509 CA Signed over AMQP and MQTT at the moment.  X509-CA authentication over websocket and HTTPS are not supported. |
| [Send device-to-cloud message](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-d2c)     | :heavy_check_mark:* | :heavy_check_mark:* | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | Send device-to-cloud messages (max 256KB) to IoT Hub with the option to add application properties and system properties, and batch send. </br> *IoT Hub only supports batch send over AMQP and HTTPS at the moment. The MQTT implementation loops over the batch and sends each message individually. |
| [Receive cloud-to-device messages](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d) | :heavy_check_mark:* | :heavy_check_mark:* | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | Receive cloud-to-device messages and read associated application and system properties from IoT Hub, with the option to complete/reject/abandon C2D messages. </br> *IoT Hub does not support the option to reject/abandon C2D messages over MQTT at the moment. |
| [Device Twins](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-device-twins)                     | :heavy_check_mark:* | :heavy_check_mark:* | :heavy_check_mark:* | :heavy_check_mark:* | :heavy_minus_sign:  | IoT Hub persists a device twin for each device that you connect to IoT Hub.  The device can perform operations like get twin tags, subscribe to desired properties. </br> *Send reported properties version and desired properties version are in progress. |
| [Direct Methods](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-direct-methods)                 | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_minus_sign:  | IoT Hub gives you the ability to invoke direct methods on devices from the cloud.  The SDK supports handler for method specific and generic operation. |
| [Upload file to Blob](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-file-upload)               | :heavy_minus_sign:  | :heavy_minus_sign:  | :heavy_minus_sign:  | :heavy_minus_sign:  | :heavy_check_mark:  | The user can use the device client to retrieve a SAS URI from IoT Hub (to use for file uploads), upload to Azure Storage blob using IoT Hub provided credentials (using a supported client library), and then use the device client to notify IoT Hub that a file upload has completed.   File upload requires HTTPS connection, but can be initiated from client using any protocol for other operations. |
| [Connection Status and Error reporting](https://docs.microsoft.com/rest/api/iothub/common-error-codes)     | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | Error reporting for IoT Hub supported error code. |
| Retry policies                                                                                                   | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | Retry policy for unsuccessful device-to-cloud messages have three options: no try, exponential backoff with jitter (default) and custom. |
| Devices multiplexing over single connection                                                                      | :heavy_minus_sign:  | :heavy_minus_sign:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  |
| Connection Pooling - Specifying number of connections                                                            | :heavy_minus_sign:  | :heavy_minus_sign:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  |
| [IoT Plug and Play][pnp-device-dev-guide]                                                                        | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:*  | :heavy_check_mark:*  | :heavy_minus_sign:  | IoT Plug and Play lets you build smart devices that advertise their capabilities to Azure IoT applications. IoT Plug and Play devices don't require manual configuration when a customer connects them to IoT Plug and Play-enabled applications. You can read more [here](https://docs.microsoft.com/azure/iot-pnp/overview-iot-plug-and-play). </br> *Note: AMQP support is mainly targeted for Edge-based scenarios. |

### IoT Hub Service SDK

:heavy_check_mark: feature available  :heavy_multiplication_x: feature planned but not supported  :heavy_minus_sign: no support planned

| Features                                                                                                      | Support                  | Transport protocol used underneath | Client to use | Description                                                                                                              |
|---------------------------------------------------------------------------------------------------------------|---------------------     |-------------------------| -------|--------------------------------------------------------------------------------------------------------------------------|
| [Identity registry (CRUD)](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-identity-registry) | :heavy_check_mark:        | HTTP | RegistryManager | Use your backend app to perform CRUD operation for individual device or in bulk.                                         ||
| [Query](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-query-language)                       | :heavy_check_mark:        | HTTP | RegistryManager | Use your backend app to query for information on device twins, module twins, jobs and message routing.                                                                   |
| [Import/Export jobs](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-jobs)                                  | :heavy_check_mark:        | HTTP | RegistryManager | Use your backend app to import or export device identities in bulk.                                                                      |
| [Scheduled jobs](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-jobs)                                  | :heavy_check_mark:        | HTTP | JobsClient | Use your backend app to schedule jobs to update desired properties, update tags and invoke direct methods.
| [Cloud-to-device messaging](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d)     | :heavy_check_mark:        | AMQP | ServiceClient | Use your backend app to send cloud-to-device messages in AMQP and AMQP-WS, and set up notifications for cloud-to-device message delivery. |
| [Direct Methods operations](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-direct-methods)   | :heavy_check_mark:        | HTTP | ServiceClient | Use your backend app to invoke direct method on device.                                                                  |
| [File Upload Notifications](https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-file-upload)                    | :heavy_check_mark:        | AMQP | ServiceClient | Use your backend app to receive file upload notifications.
| [IoT Hub Statistics](https://docs.microsoft.com/azure/iot-hub/iot-hub-metrics)                          | :heavy_check_mark:        | HTTP | ServiceClient | Use your backend app to get IoT hub identity registry statistics such as total device count for device statistics, and connected device count for service statistics.
| [Digital Twin Operations](https://docs.microsoft.com/azure/iot-pnp/overview-iot-plug-and-play)              | :heavy_check_mark:        | HTTP | DigitalTwinClient or RegistryManager | Use your backend app to perform operations on plug and play devices. The operations include get twins, update twins and invoke commands. DigitalTwinClient is the preferred client to use.

### Provisioning Device SDK

This repository contains [provisioning device client SDK](./provisioning/device) for the [Device Provisioning Service](https://docs.microsoft.com/azure/iot-dps/).

:heavy_check_mark: feature available  :heavy_multiplication_x: feature planned but not supported  :heavy_minus_sign: no support planned

| Features                    | mqtt               | mqtt-ws            | amqp               | amqp-ws            | https              | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
|-----------------------------|--------------------|--------------------|--------------------|--------------------|--------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| TPM Individual Enrollment   | :heavy_minus_sign: | :heavy_minus_sign:  | :heavy_check_mark: | :heavy_check_mark:  | :heavy_check_mark: | This SDK supports connecting your device to the Device Provisioning Service via [individual enrollment](https://docs.microsoft.com/azure/iot-dps/concepts-service#enrollment) using [Trusted Platform Module](https://docs.microsoft.com/azure/iot-dps/concepts-security#trusted-platform-module-tpm).  Please review the [samples](./provisioning/device/samples/) folder and this [quickstart](https://docs.microsoft.com/azure/iot-dps/quick-create-simulated-device-tpm-csharp) on how to create a device client.  TPM over MQTT is currently not supported by the Device Provisioning Service. |
| X.509 Individual Enrollment | :heavy_check_mark: | :heavy_check_mark:* | :heavy_check_mark: | :heavy_check_mark:* | :heavy_check_mark: | This SDK supports connecting your device to the Device Provisioning Service via [individual enrollment](https://docs.microsoft.com/azure/iot-dps/concepts-service#enrollment) using [X.509 root certificate](https://docs.microsoft.com/azure/iot-dps/concepts-security#root-certificate).  Please review the [samples](./provisioning/device/samples/) and this [quickstart](https://docs.microsoft.com/azure/iot-dps/quick-create-simulated-device-x509-csharp) folder on how to create a device client.                                                                                          |
| X.509 Enrollment Group      | :heavy_check_mark: | :heavy_check_mark:* | :heavy_check_mark: | :heavy_check_mark:* | :heavy_check_mark: | This SDK supports connecting your device to the Device Provisioning Service via [individual enrollment](https://docs.microsoft.com/azure/iot-dps/concepts-service#enrollment) using [X.509 leaf certificate](https://docs.microsoft.com/azure/iot-dps/concepts-security#leaf-certificate).  Please review the [samples](./provisioning/device/samples/) folder on how to create a device client.                                                                                                                                                                                                          |

_Note *_ WebSocket support for MQTT/AMQP is limited to .NET Framework 4.x.

### Provisioniong Service SDK

This repository contains [provisioning service client SDK](./provisioning/service/) for the Device Provisioning Service to [programmatically enroll devices](https://docs.microsoft.com/azure/iot-dps/how-to-manage-enrollments-sdks).

| Feature                                            | Support            | Description                                                                                                                                                                                             |
|----------------------------------------------------|--------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CRUD Operation with TPM Individual Enrollment      | :heavy_check_mark: | Programmatically manage device enrollment using TPM with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature.                         |
| Bulk CRUD Operation with TPM Individual Enrollment | :heavy_check_mark: | Programmatically bulk manage device enrollment using TPM with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature.                    |
| CRUD Operation with X.509 Individual Enrollment    | :heavy_check_mark: | Programmatically manage device enrollment using X.509 individual enrollment with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature. |
| CRUD Operation with X.509 Group Enrollment         | :heavy_check_mark: | Programmatically manage device enrollment using X.509 group enrollment with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature.      |
| Query enrollments                                  | :heavy_check_mark: | Programmatically query registration states with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature.                                  |

## Read more

- [Azure IoT Hub documentation][iot-hub-documentation]
- [Set up IoT Hub](doc/setup_iothub.md) describes how to configure your Azure IoT Hub service.
- [Manage IoT Hub](doc/manage_iot_hub.md) describes how to provision devices in your Azure IoT Hub service.
- [Azure Certified for IoT device catalog](https://catalog.azureiotsuite.com/)
- [Set up your development environment](./doc/devbox_setup.md) to prepare your development environment as well as how to run the samples on Linux, Windows or other platforms.
- [API reference documentation for .NET](https://docs.microsoft.com/dotnet/api/overview/azure/devices?view=azure-dotnet)
- [Get Started with IoT Hub using .NET](https://docs.microsoft.com/azure/iot-hub/iot-hub-csharp-csharp-getstarted)

> Device Explorer is no longer supported. A replacement tool can be found [here](https://github.com/Azure/azure-iot-explorer).

## Certificates -  Important to know

The Azure IoT Hub certificates presented during TLS negotiation shall be always validated using the appropriate root CA certificate(s).

Always prefer using the local system's Trusted Root Certificate Authority store instead of hardcoding the certificates. 

A couple of examples:

- Windows: Schannel will automatically pick up CA certificates from the store managed using `certmgr.msc`.
- Debian Linux: OpenSSL will automatically pick up CA certificates from the store installed using `apt install ca-certificates`. Adding a certificate to the store is described here: http://manpages.ubuntu.com/manpages/precise/man8/update-ca-certificates.8.html

### Additional Information

For additional guidance and important information about certificates, please refer to [this blog post](https://techcommunity.microsoft.com/t5/internet-of-things/azure-iot-tls-changes-are-coming-and-why-you-should-care/ba-p/1658456) from the security team.

# Long-Term Support (LTS)

The project offers a Long Term Support (LTS) version to allow users that do not need the latest features to be shielded from unwanted changes.

As of August 2020, the .NET SDK is shifting to a revised Long-Term Support strategy.
The primary motivations for this change are to extend the support period and decrease the churn on LTS releases, while still maintaining a strategy that offers customers choice between new features and stability.

We now will be releasing a new LTS branch yearly, and each LTS release will be supported for 3 years - 1 year of active maintenance with bugfixes, and 2 years of extended support for security fixes.

LTS branches receive all bug fixes that fall in one of these categories:

- security bugfixes
- critical bugfixes (crashes, memory leaks, etc.)

No new features or improvements will be picked up in an LTS branch. A patch will not extend the maintenance or expiry date.

LTS branches are named lts_*yyyy*_*mm*, where *mm* and *yyyy* are the month and year when the branch was created. An example of such a branch is *lts_2018_01*.

## Schedule<sup>1</sup>

Below is a table showing the mapping of the LTS branches to the packages released.

| Release                                                                                       | Github Branch | LTS Status  | LTS Start Date | Maintenance End Date | LTS End Date |
| :-------------------------------------------------------------------------------------------: | :-----------: | :--------:  | :------------: | :------------------: | :----------: |
| [2021-10-19](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2021-3-18-patch3)  | [lts_2021_03](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2021_03)   | Active      | 2021-10-19     | 2022-03-18           | 2024-03-17   |
| [2021-8-12](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2021-3-18_patch2)  | [lts_2021_03](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2021_03)   | Active      | 2021-08-12     | 2022-03-18           | 2024-03-17   |
| [2021-8-10](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-8-19_patch2)  | [lts_2020_08](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2020_08)   | Active      | 2021-08-10     | 2021-08-19           | 2023-08-19   |
| [2021-6-23](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2021-3-18_patch1)  | [lts_2021_03](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2021_03)   | Active      | 2020-06-23     | 2022-03-18           | 2024-03-17   |
| [2021-3-18](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2021-3-18)         | [lts_2021_03](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2021_03)   | Active      | 2020-03-18     | 2022-03-18           | 2024-03-17   |
| [2020-9-23](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-8-19_patch1)  | [lts_2020_08](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2020_08)   | Active      | 2020-09-23     | 2021-08-19           | 2023-08-19   |
| [2020-8-19](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-8-19)         | [lts_2020_08](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2020_08)   | Active      | 2020-08-19     | 2021-08-19           | 2023-08-19   |
| [2020-4-3](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-1-31_patch1)   | [lts_2020_01](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2020_01)   | Depreciated | 2020-04-03     | 2021-01-30           | 2023-01-30   |
| [2020-1-31](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-1-31)         | [lts_2020_01](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2020_01)   | Depreciated | 2020-01-31     | 2021-01-30           | 2023-01-30   |

- <sup>1</sup> All scheduled dates are subject to change by the Azure IoT SDK team.

### Planned release schedule
![](./lts_branches.png)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

Microsoft collects performance and usage information which may be used to provide and improve Microsoft products and services and enhance your experience.
To learn more, review the [privacy statement](https://go.microsoft.com/fwlink/?LinkId=521839&clcid=0x409).

[iot-hub-documentation]: https://docs.microsoft.com/azure/iot-hub/
[iot-dev-center]: http://azure.com/iotdev
[azure-iot-sdks]: https://github.com/azure/azure-iot-sdks
[dotnet-api-reference]: https://docs.microsoft.com/dotnet/api/overview/azure/iot/client?view=azure-dotnet
[iothub-device-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Client.svg?style=plastic
[iothub-device-prerelease]: https://img.shields.io/nuget/vpre/Microsoft.Azure.Devices.Client.svg?style=plastic
[iothub-device-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Client/
[iothub-service-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.svg?style=plastic
[iothub-service-prerelease]: https://img.shields.io/nuget/vpre/Microsoft.Azure.Devices.svg?style=plastic
[iothub-service-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices/
[iothub-shared-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Shared.svg?style=plastic
[iothub-shared-prerelease]: https://img.shields.io/nuget/vpre/Microsoft.Azure.Devices.Shared.svg?style=plastic
[iothub-shared-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Shared/
[dps-device-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Provisioning.Client.svg?style=plastic
[dps-device-prerelease]: https://img.shields.io/nuget/vpre/Microsoft.Azure.Devices.Provisioning.Client.svg?style=plastic
[dps-device-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Provisioning.Client/
[dps-device-amqp-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Provisioning.Transport.Amqp.svg?style=plastic
[dps-device-amqp-prerelease]: https://img.shields.io/nuget/vpre/Microsoft.Azure.Devices.Provisioning.Transport.Amqp.svg?style=plastic
[dps-device-amqp-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Provisioning.Transport.Amqp/
[dps-device-http-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Provisioning.Transport.Http.svg?style=plastic
[dps-device-http-prerelease]: https://img.shields.io/nuget/vpre/Microsoft.Azure.Devices.Provisioning.Transport.Http.svg?style=plastic
[dps-device-http-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Provisioning.Transport.Http/
[dps-device-mqtt-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Provisioning.Transport.Mqtt.svg?style=plastic
[dps-device-mqtt-prerelease]: https://img.shields.io/nuget/vpre/Microsoft.Azure.Devices.Provisioning.Transport.Mqtt.svg?style=plastic
[dps-device-mqtt-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Provisioning.Transport.Mqtt/
[dps-service-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Provisioning.Service.svg?style=plastic
[dps-service-prerelease]: https://img.shields.io/nuget/vpre/Microsoft.Azure.Devices.Provisioning.Service.svg?style=plastic
[dps-service-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Provisioning.Service/
[dps-tpm-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Provisioning.Security.Tpm.svg?style=plastic
[dps-tpm-prerelease]: https://img.shields.io/nuget/vpre/Microsoft.Azure.Devices.Provisioning.Security.Tpm.svg?style=plastic
[dps-tpm-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Provisioning.Security.Tpm/
[pnp-device-prerelease]: https://img.shields.io/nuget/vpre/Microsoft.Azure.Devices.DigitalTwin.Client.svg?style=plastic
[pnp-device-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.DigitalTwin.Client/
[pnp-service-prerelease]: https://img.shields.io/nuget/vpre/Microsoft.Azure.Devices.DigitalTwin.Service.svg?style=plastic
[pnp-service-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.DigitalTwin.Service/
[pnp-device-dev-guide]: https://docs.microsoft.com/azure/iot-pnp/concepts-developer-guide-device?pivots=programming-language-csharp
