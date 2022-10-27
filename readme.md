# Microsoft Azure IoT SDK for .NET

## V2 clients release notice

The [2.0.0-preview001](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/2022-10-14-v2preview) release has been published for each package in this library. 
These packages simplify usage and handling of our clients, and we highly encourage you to try out these packages and share your feedback. Once we promote these 
changes to `main` in the future and release the 2.x.x packages publicly, all future features will be brought only to this new major version.

If you need any help migrating your code to try out the new 2.X.X clients, please see this [migration guide](https://github.com/Azure/azure-iot-sdk-csharp/blob/previews/v2/SDK%20v2%20migration%20guide.md).

## Contents

This repository contains the following:

- **Microsoft Azure IoT Hub device SDK for C#** to connect client devices to Azure IoT Hub with .NET.
- **Microsoft Azure IoT Hub service SDK for C#** to manage your IoT Hub service instance from a back-end .NET application.
- **Microsoft Azure Provisioning device SDK for C#** to provision devices to Azure IoT Hub with .NET.
- **Microsoft Azure Provisioning service SDK for C#** to manage your Provisioning service instance from a back-end .NET application.

## Critical Upcoming Change Notice

All Azure IoT SDK users are advised to be aware of upcoming TLS certificate changes for Azure IoT Hub and Device Provisioning Service 
that will impact the SDK's ability to connect to these services. In October 2022, both services will migrate from the current 
[Baltimore CyberTrust CA Root](https://baltimore-cybertrust-root.chain-demos.digicert.com/info/index.html) to the 
[DigiCert Global G2 CA root](https://global-root-g2.chain-demos.digicert.com/info/index.html). There will be a 
transition period beforehand where your IoT devices must have both the Baltimore and Digicert public certificates 
installed in their certificate store in order to prevent connectivity issues. 

**Devices with only the Baltimore public certificate installed will lose the ability to connect to Azure IoT hub and Device Provisioning Service in October 2022.**

To prepare for this change, make sure your device's certificate store has both of these public certificates installed.

For a more in depth explanation as to why the IoT services are doing this, please see
[this article](https://techcommunity.microsoft.com/t5/internet-of-things/azure-iot-tls-critical-changes-are-almost-here-and-why-you/ba-p/2393169).

## Recommended NuGet packages

| Package Name                                          | Release Version                                           |
| ---                                                   | ---                                                       |
| Microsoft.Azure.Devices.Client                        | [![NuGet][iothub-device-release]][iothub-device-nuget]    |
| Microsoft.Azure.Devices                               | [![NuGet][iothub-service-release]][iothub-service-nuget]  |
| Microsoft.Azure.Devices.Shared                        | [![NuGet][iothub-shared-release]][iothub-shared-nuget]    |
| Microsoft.Azure.Devices.Provisioning.Client           | [![NuGet][dps-device-release]][dps-device-nuget]          |
| Microsoft.Azure.Devices.Provisioning.Transport.Amqp   | [![NuGet][dps-device-amqp-release]][dps-device-amqp-nuget]|
| Microsoft.Azure.Devices.Provisioning.Transport.Http   | [![NuGet][dps-device-http-release]][dps-device-http-nuget]|
| Microsoft.Azure.Devices.Provisioning.Transport.Mqtt   | [![NuGet][dps-device-mqtt-release]][dps-device-mqtt-nuget]|
| Microsoft.Azure.Devices.Provisioning.Service          | [![NuGet][dps-service-release]][dps-service-nuget]        |
| Microsoft.Azure.Devices.Provisioning.Security.Tpm     | [![NuGet][dps-tpm-release]][dps-tpm-nuget]                |

> Note:  
> 1. In addition to stable builds we also release pre-release builds that contain preview features. You can find details about the preview features released by looking at the [release notes](https://github.com/Azure/azure-iot-sdk-csharp/releases). It is not recommended to take dependency on preview NuGets for production applications as breaking changes can be introduced in preview packages.
> 2. Device streaming feature is not being included in our newer preview releases as there is no active development going on in the service. For more details on the feature, see [here](https://docs.microsoft.com/azure/iot-hub/iot-hub-device-streams-overview).
>  
>       This feature has not been included in any preview release after [2020-10-14](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/preview_2020-10-14). However, the feature is still available under [previews/deviceStreaming branch](https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/deviceStreaming).  
>  
>       The latest preview NuGet versions that contain the device streaming feature are:  
        Microsoft.Azure.Devices.Client - 1.32.0-preview-001  
        Microsoft.Azure.Devices - 1.28.0-preview-001
> 3. Stable and preview NuGet versions are not interdependent; eg. for NuGet packages versioned 1.25.0 (stable release) and 1.25.0-preview-001 (preview release), there is no guarantee that v1.25.0 contains the feature(s) previewed in v1.25.0-preview-001. For a list of updates shipped with each NuGet package, please refer to the [release notes](https://github.com/Azure/azure-iot-sdk-csharp/releases).

The API reference documentation for .NET SDK is [here][dotnet-api-reference].

To find SDKs in other languages for Azure IoT, please refer to the [azure-iot-sdks][azure-iot-sdks] repository.
For IoT Hub Management SDK in .NET, please visit [azure-sdk-for-net](https://github.com/Azure/azure-sdk-for-net) repository.

## Need support?

- Have a feature request for SDKs? Please post it on [User Voice](https://feedback.azure.com/forums/321918-azure-iot) to help us prioritize.
- Have a technical question? Ask on [Stack Overflow](https://stackoverflow.com/questions/tagged/azure-iot-hub) with tag “azure-iot-hub”.
- Need Support? Every customer with an active Azure subscription has access to [support](https://docs.microsoft.com/azure/azure-supportability/how-to-create-azure-support-request) with guaranteed response time. Consider submitting a ticket and get assistance from Microsoft support team.
- Found a bug? Please help us fix it by thoroughly documenting it and filing an issue on GitHub (C, Java, .NET, Node.js, Python).

## Developing applications for Azure IoT

Visit [Azure IoT Dev Center][iot-dev-center] to learn more about developing applications for Azure IoT.

## Samples

All of our samples are located in this repository. The samples live alongside the source for each client library.

- [IoT hub device](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/iothub/device/samples) samples
- [IoT hub service](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/iothub/service/samples) samples
- [Provisioning device](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/device/samples) samples
- [Provisioning service](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/service/samples) samples

Samples for each of these categories are further separated into three sub-categories (from simplest to complex):

1. `Getting Started`
2. `How To`
3. `Solutions`

If you are looking for a best practice solution sample using X.509 authentication to get started with building your own custom IoT cloud solution, please see [best practice X.509 solution](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/device/samples/solutions/BestPracticeSampleX509) sample.

If you are looking for a good device sample to get started with, please see the [device reconnection sample](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/iothub/device/samples/how%20to%20guides/DeviceReconnectionSample).
It shows how to connect a device, handle disconnect events, cases to handle when making calls, and when to re-initialize the `DeviceClient`.

## Contribute to the Azure IoT C# SDK

If you would like to build or change the SDK source code, please follow the [devguide](doc/devguide.md).

## OS platforms and hardware compatibility

For an official list of all the operating systems and .NET platforms that we support, please see [this document](./supported_platforms.md).

Note that you can configure your TLS protocol version and ciphers by following [this document](./configure_tls_protocol_version_and_ciphers.md).

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
- [Device connection and messaging reliability](https://github.com/Azure/azure-iot-sdk-csharp/blob/main/device_connection_and_reliability_readme.md)

> Device Explorer is no longer supported. A replacement tool can be found [here](https://github.com/Azure/azure-iot-explorer).

## Certificates -  Important to know

The Azure IoT Hub certificates presented during TLS negotiation shall be always validated using the appropriate root CA certificate(s).

Always prefer using the local system's Trusted Root Certificate Authority store instead of hardcoding the certificates. 

A couple of examples:

- Windows: Schannel will automatically pick up CA certificates from the store managed using `certmgr.msc`.
- Debian Linux: OpenSSL will automatically pick up CA certificates from the store installed using `apt install ca-certificates`. Adding a certificate to the store is described here: http://manpages.ubuntu.com/manpages/precise/man8/update-ca-certificates.8.html

### Additional Information

For additional guidance and important information about certificates, please refer to [this blog post](https://techcommunity.microsoft.com/t5/internet-of-things/azure-iot-tls-changes-are-coming-and-why-you-should-care/ba-p/1658456) from the security team.

## Long-Term Support (LTS)

The project offers a Long-Term Support (LTS) releases to allow users that do not need the latest features to be shielded from unwanted changes.

LTS repo tags are to be named lts_*yyyy*-*mm*-*dd*, where *yyyy*, *mm*, and *dd* are the year, month, and day when the tag was created. An example of such a tag is *lts_2021-03-18*.

The lifetime of an LTS release is 12 months. During this time, LTS releases may receive maintenance bug fixes that fall in these categories:

- security bug fixes
- critical bug fixes (e.g., unavoidable/unrecoverable crashes, significant memory leaks)

> No new features or improvements are in scope to be picked up in an LTS branch. A patch will not extend the maintenance or expiry date.

LTS releases may include additional extended support for security bug fixes as listed in the LTS schedule.

### Schedule

This table shows previous LTS releases and end dates.

| Release                                                                                                                        | LTS Start Date | Maintenance End Date |
| :----------------------------------------------------------------------------------------------------------------------------: | :------------: | :------------------: |
| [2022-06-07](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2021-3-18_patch6) <sub>patch 6 of 2021-03-18</sub> | 2021-03-18     | current              |
| [2020-9-23](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-8-19_patch1) <sub>patch 1 of 2020-08-19</sub>  | 2020-08-19     | 2021-08-19           |
| [2020-4-3](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-1-31_patch1) <sub>patch 1 of 2020-01-31</sub>   | 2020-01-31     | 2021-01-30           |

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
[iothub-device-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Client/
[iothub-service-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.svg?style=plastic
[iothub-service-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices/
[iothub-shared-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Shared.svg?style=plastic
[iothub-shared-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Shared/
[dps-device-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Provisioning.Client.svg?style=plastic
[dps-device-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Provisioning.Client/
[dps-device-amqp-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Provisioning.Transport.Amqp.svg?style=plastic
[dps-device-amqp-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Provisioning.Transport.Amqp/
[dps-device-http-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Provisioning.Transport.Http.svg?style=plastic
[dps-device-http-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Provisioning.Transport.Http/
[dps-device-mqtt-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Provisioning.Transport.Mqtt.svg?style=plastic
[dps-device-mqtt-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Provisioning.Transport.Mqtt/
[dps-service-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Provisioning.Service.svg?style=plastic
[dps-service-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Provisioning.Service/
[dps-tpm-release]: https://img.shields.io/nuget/v/Microsoft.Azure.Devices.Provisioning.Security.Tpm.svg?style=plastic
[dps-tpm-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.Provisioning.Security.Tpm/
[pnp-device-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.DigitalTwin.Client/
[pnp-service-nuget]: https://www.nuget.org/packages/Microsoft.Azure.Devices.DigitalTwin.Service/
[pnp-device-dev-guide]: https://docs.microsoft.com/azure/iot-pnp/concepts-developer-guide-device?pivots=programming-language-csharp

