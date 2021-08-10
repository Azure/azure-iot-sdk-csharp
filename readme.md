# Microsoft Azure IoT SDK for .NET

### Help us help you with our IoT SDKs!
We are running a survey through August 2020 to learn more about your IoT projects and support needs. Our team will use this information to help shape the future of our IoT SDKs, and, if you choose to provide your contact information, we'll include you in our circle of advisors for early feedback. Consider spending ~5 minutes completing **[this survey](https://aka.ms/iotsdksurvey)**-- we'd love to hear from you!

### Contents
This repository contains the following:

- **Microsoft Azure IoT Hub device SDK for C#** to connect client devices to Azure IoT Hub with .NET
- **Microsoft Azure IoT Hub service SDK for C#** to manage your IoT Hub service instance from a back-end .NET application
- **Microsoft Azure Provisioning device SDK for C#** to provision devices to Azure IoT Hub with .NET
- **Microsoft Azure Provisioning service SDK for C#** to manage your Provisioning service instance from a back-end .NET application

### Build status

Due to security considerations, build logs are not publicly available.

| Service Environment                                                   | Status |
| ---                                                                   | ---    |
| [Master](https://github.com/Azure/azure-iot-sdk-csharp/tree/master)   | [![Build Status](https://azure-iot-sdks.visualstudio.com/azure-iot-sdks/_apis/build/status/csharp/CSharp%20Prod%20-%20West%20Central%20US?branchName=master)](https://azure-iot-sdks.visualstudio.com/azure-iot-sdks/_build/latest?definitionId=44&repositoryFilter=9&branchName=master)|
| [Preview](https://github.com/Azure/azure-iot-sdk-csharp/tree/preview) | [![Build Status](https://azure-iot-sdks.visualstudio.com/azure-iot-sdks/_apis/build/status/csharp/CSharp%20Canary%20-%20Central%20US%20EUAP?branchName=preview)](https://azure-iot-sdks.visualstudio.com/azure-iot-sdks/_build/latest?definitionId=402&repositoryFilter=9&branchName=preview)|

### Recommended NuGet packages

| Package Name                                          | Release Version                                           | Pre-release Version   |
| ---                                                   | ---                                                       | ---                   |
| Microsoft.Azure.Devices.Client                        | [![NuGet][iothub-device-release]][iothub-device-nuget]    | [![NuGet][iothub-device-prerelease]][iothub-device-nuget]     |
| Microsoft.Azure.Devices                               | [![NuGet][iothub-service-release]][iothub-service-nuget]  | [![NuGet][iothub-service-prerelease]][iothub-service-nuget]   |
| Microsoft.Azure.Devices.Shared                        | [![NuGet][iothub-shared-release]][iothub-shared-nuget]    | [![NuGet][iothub-shared-prerelease]][iothub-shared-nuget]     |
| Microsoft.Azure.Devices.Provisioning.Client           | [![NuGet][dps-device-release]][dps-device-nuget]          | [![NuGet][dps-device-prerelease]][dps-device-nuget]       |
| Microsoft.Azure.Devices.Provisioning.Transport.Amqp   | [![NuGet][dps-device-amqp-release]][dps-device-amqp-nuget]| [![NuGet][dps-device-amqp-prerelease]][dps-device-amqp-nuget] |
| Microsoft.Azure.Devices.Provisioning.Transport.Http   | [![NuGet][dps-device-http-release]][dps-device-http-nuget]| [![NuGet][dps-device-http-prerelease]][dps-device-http-nuget] |
| Microsoft.Azure.Devices.Provisioning.Transport.Mqtt   | [![NuGet][dps-device-mqtt-release]][dps-device-mqtt-nuget]| [![NuGet][dps-device-mqtt-prerelease]][dps-device-mqtt-nuget] |
| Microsoft.Azure.Devices.Provisioning.Service          | [![NuGet][dps-service-release]][dps-service-nuget]        | [![NuGet][dps-service-prerelease]][dps-service-nuget]     |
| Microsoft.Azure.Devices.Provisioning.Security.Tpm     | [![NuGet][dps-tpm-release]][dps-tpm-nuget]                | [![NuGet][dps-tpm-prerelease]][dps-tpm-nuget]     |
| Microsoft.Azure.Devices.DigitalTwin.Client            | NA                                                        | [![NuGet][pnp-device-prerelease]][pnp-device-nuget]  |
| Microsoft.Azure.Devices.DigitalTwin.Service           | NA                                                        | [![NuGet][pnp-service-prerelease]][pnp-service-nuget] |

The API reference documentation for .NET SDK is [here][dotnet-api-reference].

To find SDKs in other languages for Azure IoT, please refer to the [azure-iot-sdks][azure-iot-sdks] repository.
For IoT Hub Management SDK in .NET, please visit [azure-sdk-for-net](https://github.com/Azure/azure-sdk-for-net) repository

## Need Support?

- Have a feature request for SDKs? Please post it on [User Voice](https://feedback.azure.com/forums/321918-azure-iot) to help us prioritize.
- Have a technical question? Ask on [Stack Overflow](https://stackoverflow.com/questions/tagged/azure-iot-hub) with tag “azure-iot-hub”
- Need Support? Every customer with an active Azure subscription has access to [support](https://docs.microsoft.com/en-us/azure/azure-supportability/how-to-create-azure-support-request) with guaranteed response time.  Consider submitting a ticket and get assistance from Microsoft support team.
- Found a bug? Please help us fix it by thoroughly documenting it and filing an issue on GitHub (C, Java, .NET, Node.js, Python).

## Developing applications for Azure IoT

Visit [Azure IoT Dev Center][iot-dev-center] to learn more about developing applications for Azure IoT.

## Samples

Samples are available at [Azure IoT Samples for C#](https://github.com/Azure-Samples/azure-iot-samples-csharp)

## Contribute to the Azure IoT C# SDK

If you would like to build or change the SDK source code, please follow the [devguide](doc/devguide.md).

## OS platforms and hardware compatibility

> .NET MicroFramework will not be supported in future versions of the SDK.

> .NET Standard 1.3 (IoT Hub SDKs only) is last supported in the [2020-02-27](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/2020-2-27) and in the [2020-1-31 LTS](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-1-31) releases.

The IoT Hub device SDK for .NET can be used with a broad range of OS platforms and devices.

The NuGet packages provide support for the following .NET flavors:
- .NET Standard 2.1
- .NET Standard 2.0
- .NET Framework 4.7.2 (IoT Hub SDKs only)
- .NET Framework 4.5.1 (IoT Hub SDKs only)

For details on .NET support see the [.NET Standard documentation](https://docs.microsoft.com/en-us/dotnet/standard/net-standard).
For details on OS support see the following resources:

- [.NET Core Runtime ID Catalog](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)
- [.NET Framework System Requirements](https://docs.microsoft.com/en-us/dotnet/framework/get-started/system-requirements)
- [Configure TLS Protocol Version and Ciphers](./configure_tls_protocol_version_and_ciphers.md)

## Key features and roadmap

### IoT Hub Device SDK

:heavy_check_mark: feature available  :heavy_multiplication_x: feature planned but not supported  :heavy_minus_sign: no support planned

| Features                                                                                                         | mqtt                | mqtt-ws             | amqp                | amqp-ws             | https               | Description                                                                                                                                                                                                                                                            |
|------------------------------------------------------------------------------------------------------------------|---------------------|---------------------|---------------------|---------------------|---------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [Authentication](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-security-deployment)                     | :heavy_check_mark: | :heavy_check_mark:* | :heavy_check_mark: | :heavy_check_mark:* | :heavy_check_mark:* | Connect your device to IoT Hub securely with supported authentication, including private key, SASToken, X-509 Self Signed and X-509 CA Signed. *IoT Hub only supports X-509 CA Signed over AMQP and MQTT at the moment.  X509-CA authentication over websocket and HTTPS are not supported.                                                                               |
| [Send device-to-cloud message](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-d2c)     | :heavy_check_mark:* | :heavy_check_mark:* | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | Send device-to-cloud messages (max 256KB) to IoT Hub with the option to add custom properties and system properties, and batch send.  *IoT Hub only supports batch send over AMQP and HTTPS at the moment.                                                             |
| [Receive cloud-to-device messages](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-c2d) | :heavy_check_mark:* | :heavy_check_mark:* | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | Receive cloud-to-device messages and read associated custom and system properties from IoT Hub, with the option to complete/reject/abandon C2D messages.  *IoT Hub supports the option to complete/reject/abandon C2D messages over HTTPS and AMQP only at the moment. |
| [Device Twins](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-device-twins)                     | :heavy_check_mark:* | :heavy_check_mark:* | :heavy_check_mark:* | :heavy_check_mark:* | :heavy_minus_sign:  | IoT Hub persists a device twin for each device that you connect to IoT Hub.  The device can perform operations like get twin tags, subscribe to desired properties.  *Send reported properties version and desired properties version are in progress.                 |
| [Direct Methods](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods)                 | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_minus_sign:  | IoT Hub gives you the ability to invoke direct methods on devices from the cloud.  The SDK supports handler for method specific amd generic operation.                                                                                                                 |
| [Upload file to Blob](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-file-upload)               | :heavy_minus_sign:  | :heavy_minus_sign:  | :heavy_minus_sign:  | :heavy_minus_sign:  | :heavy_check_mark:  | A device can initiate a file upload and notifies IoT Hub when the upload is complete.   File upload requires HTTPS connection, but can be initiated from client using any protocol for other operations.                                                               |
| [Connection Status and Error reporting](https://docs.microsoft.com/en-us/rest/api/iothub/common-error-codes)     | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | Error reporting for IoT Hub supported error code.                                                                                                                                                                                                                      |
| Retry policies                                                                                                   | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  | Retry policy for unsuccessful device-to-cloud messages have three options: no try, exponential backoff with jitter (default) and custom.                                                                                                                               |
| Devices multiplexing over single connection                                                                      | :heavy_minus_sign:  | :heavy_minus_sign:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  |                                                                                                                                                                                                                                                                        |
| Connection Pooling - Specifying number of connections                                                            | :heavy_minus_sign:  | :heavy_minus_sign:  | :heavy_check_mark:  | :heavy_check_mark:  | :heavy_check_mark:  |                                                                                                                                                                                                                                                                        |

### IoT Hub Service SDK

:heavy_check_mark: feature available  :heavy_multiplication_x: feature planned but not supported  :heavy_minus_sign: no support planned

| Features                                                                                                      | C# .Net             | Description                                                                                                              |
|---------------------------------------------------------------------------------------------------------------|---------------------|--------------------------------------------------------------------------------------------------------------------------|
| [Identity registry (CRUD)](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-identity-registry) | :heavy_check_mark:* | Use your backend app to perform CRUD operation for individual device or in bulk.                                         |
| [Cloud-to-device messaging](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-c2d)     | :heavy_check_mark:  | Use your backend app to send cloud-to-device messages in AMQP and AMQP-WS, and set up cloud-to-device message receivers. |
| [Direct Methods operations](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods)   | :heavy_check_mark:  | Use your backend app to invoke direct method on device.                                                                  |
| [Device Twins operations](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-device-twins)       | :heavy_multiplication_x:                 | Use your backend app to perform twin operations.  This SDK only supports Get Twin at the moment.                         |
| [Query](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-query-language)                       | :heavy_check_mark:  | Use your backend app to perform query for information.                                                                   |
| [Jobs](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-jobs)                                  | :heavy_check_mark:  | Use your backend app to perform job operation.                                                                           |
| [File Upload](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-file-upload)                    | :heavy_check_mark:  | Set up your backend app to send file upload notification receiver.                                                       |

### Provisioning Device SDK

This repository contains [provisioning device client SDK](./provisioning/device) for the [Device Provisioning Service](https://docs.microsoft.com/en-us/azure/iot-dps/).

:heavy_check_mark: feature available  :heavy_multiplication_x: feature planned but not supported  :heavy_minus_sign: no support planned

| Features                    | mqtt               | mqtt-ws            | amqp               | amqp-ws            | https              | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
|-----------------------------|--------------------|--------------------|--------------------|--------------------|--------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| TPM Individual Enrollment   | :heavy_minus_sign: | :heavy_minus_sign: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | This SDK supports connecting your device to the Device Provisioning Service via [individual enrollment](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-service#enrollment) using [Trusted Platform Module](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-security#trusted-platform-module-tpm).  Please review the [samples](./provisioning/device/samples/) folder and this [quickstart](https://docs.microsoft.com/en-us/azure/iot-dps/quick-create-simulated-device-tpm-csharp) on how to create a device client.  TPM over MQTT is currently not supported by the Device Provisioning Service.                                                                                                                                                                                                     |
| X.509 Individual Enrollment | :heavy_check_mark: |:heavy_check_mark:* | :heavy_check_mark: | :heavy_check_mark:* | :heavy_check_mark: | This SDK supports connecting your device to the Device Provisioning Service via [individual enrollment](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-service#enrollment) using [X.509 root certificate](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-security#root-certificate).  Please review the [samples](./provisioning/device/samples/) and this [quickstart](https://docs.microsoft.com/en-us/azure/iot-dps/quick-create-simulated-device-x509-csharp) folder on how to create a device client.   |
| X.509 Enrollment Group      | :heavy_check_mark: | :heavy_check_mark:* | :heavy_check_mark: | :heavy_check_mark:* | :heavy_check_mark: | This SDK supports connecting your device to the Device Provisioning Service via [individual enrollment](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-service#enrollment) using [X.509 leaf certificate](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-security#leaf-certificate).  Please review the [samples](./provisioning/device/samples/) folder on how to create a device client.                                                                                                                                                                                            |

_Note *_ WebSocket support for MQTT/AMQP is limited to .NET Framework 4.x.

### Provisioniong Service SDK

This repository contains [provisioning service client SDK](./provisioning/service/) for the Device Provisioning Service to [programmatically enroll devices](https://docs.microsoft.com/en-us/azure/iot-dps/how-to-manage-enrollments-sdks).

| Feature                                            | Support            | Description                                                                                                                                                                                                                                            |
|----------------------------------------------------|--------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CRUD Operation with TPM Individual Enrollment      | :heavy_check_mark: | Programmatically manage device enrollment using TPM with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature. |
| Bulk CRUD Operation with TPM Individual Enrollment | :heavy_check_mark: | Programmatically bulk manage device enrollment using TPM with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature. |
| CRUD Operation with X.509 Individual Enrollment    | :heavy_check_mark: | Programmatically manage device enrollment using X.509 individual enrollment with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature. |
| CRUD Operation with X.509 Group Enrollment         | :heavy_check_mark: | Programmatically manage device enrollment using X.509 group enrollment with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature. |
| Query enrollments                                  | :heavy_check_mark: | Programmatically query registration states with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature.                                                                            |

## Read more

- [Azure IoT Hub documentation][iot-hub-documentation]
- [Set up IoT Hub](doc/setup_iothub.md) describes how to configure your Azure IoT Hub service.
- [Manage IoT Hub](doc/manage_iot_hub.md) describes how to provision devices in your Azure IoT Hub service.
- [Azure Certified for IoT device catalog](https://catalog.azureiotsuite.com/)
- [Set up your development environment](./doc/devbox_setup.md) to prepare your development environment as well as how to run the samples on Linux, Windows or other platforms.
- [API reference documentation for .NET](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/devices?view=azure-dotnet)
- [Get Started with IoT Hub using .NET](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted)

> Device Explorer is no longer supported. A replacement tool can be found [here](https://github.com/Azure/azure-iot-explorer).

# Long Term Support

The project offers a Long Term Support (LTS) version to allow users that do not need the latest features to be shielded from unwanted changes.

As of August 2020, the .NET SDK is shifting to a revised Long-Term Support strategy. The primary motivations for this change are to extend the support period and decrease the churn on LTS releases, while still maintaining a strategy that offers customers choice between new features and stability. 

We now will be releasing a new LTS branch yearly, and each LTS release will be supported for 3 years - 1 year of active maintenance with bugfixes, and 2 years of extended support for security fixes.

LTS branches receive all bug fixes that fall in one of these categories:

- security bugfixes
- critical bugfixes (crashes, memory leaks, etc.)

No new features or improvements will be picked up in an LTS branch.

LTS branches are named lts_*yyyy*_*mm*, where *mm* and *yyyy* are the month and year when the branch was created. An example of such a branch is *lts_2018_01*.

## Schedule<sup>1</sup>

Below is a table showing the mapping of the LTS branches to the packages released

| Release | Github Branch | LTS Status | LTS Start Date | Maintenance End Date | Removed Date |
| :-----------: | :-----------: | :--------: | :------------: | :------------------: | :----------: |
| [2021-8-10](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-8-19_patch2)  | [lts_2020_08](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2020_08)   | Active      | 2021-08-10     | 2021-08-19           | 2023-08-19   |
| [2020-9-23](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-8-19_patch1)  | [lts_2020_08](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2020_08)   | Active      | 2020-09-23     | 2021-08-19           | 2023-08-19   |
| [2020-8-19](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-8-19)         | [lts_2020_08](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2020_08)   | Active      | 2020-08-19     | 2021-08-19           | 2023-08-19   |
| [2020-4-3](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-1-31_patch1)  | [lts_2020_01](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2020_01)   | Deprecated | 2020-04-03     | 2021-01-30           | 2023-01-30   |
| [2020-1-31](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-1-31)         | [lts_2020_01](https://github.com/Azure/azure-iot-sdk-csharp/tree/lts_2020_01)   | Deprecated | 2020-01-31     | 2021-01-30           | 2023-01-30   |

- <sup>1</sup> All scheduled dates are subject to change by the Azure IoT SDK team.

### Planned release schedule
![](./lts_branches.png)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

Microsoft collects performance and usage information which may be used to provide and improve Microsoft products and services and enhance your experience.  To learn more, review the [privacy statement](https://go.microsoft.com/fwlink/?LinkId=521839&clcid=0x409).  

[iot-hub-documentation]: https://docs.microsoft.com/en-us/azure/iot-hub/
[iot-dev-center]: http://azure.com/iotdev
[azure-iot-sdks]: https://github.com/azure/azure-iot-sdks
[dotnet-api-reference]: https://docs.microsoft.com/en-us/dotnet/api/overview/azure/iot/client?view=azure-dotnet
[devbox-setup]: ./doc/devbox_setup.md
[get-started-dotnet]: https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted
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
