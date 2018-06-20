# Microsoft Azure IoT SDK for .NET

This repository contains the following:
* **Microsoft Azure IoT Hub device SDK for C#** to connect client devices to Azure IoT Hub with .NET
* **Microsoft Azure IoT Hub service SDK for C#** to manage your IoT Hub service instance from a back-end .NET application
* **Microsoft Azure Provisioning device SDK for C#** to provision devices to Azure IoT Hub with .NET
* **Microsoft Azure Provisioning service SDK for C#** to manage your Provisioning service instance from a back-end .NET application

The API reference documentation for .NET SDK is [here][dotnet-api-reference].

To find SDKs in other languages for Azure IoT, please refer to the [azure-iot-sdks][azure-iot-sdks] repository.
For IoT Hub Management SDK in .NET, please visit [azure-sdk-for-net](https://github.com/Azure/azure-sdk-for-net) repository

## Need Support?
* Have a feature request for SDKs? Please post it on [User Voice](https://feedback.azure.com/forums/321918-azure-iot) to help us prioritize.
* Have a technical question? Ask on [Stack Overflow](https://stackoverflow.com/questions/tagged/azure-iot-hub) with tag “azure-iot-hub”
* Need Support? Every customer with an active Azure subscription has access to [support](https://docs.microsoft.com/en-us/azure/azure-supportability/how-to-create-azure-support-request) with guaranteed response time.  Consider submitting a ticket and get assistance from Microsoft support team.
* Found a bug? Please help us fix it by thoroughly documenting it and filing an issue on GitHub (C, Java, .NET, Node.js, Python).

## Developing applications for Azure IoT
Visit [Azure IoT Dev Center][iot-dev-center] to learn more about developing applications for Azure IoT.

## Samples
In the repository, you will find a set of samples that will help you get started:
* [Azure IoT Samples for C#](https://github.com/Azure-Samples/azure-iot-samples-csharp)
* [IoT Hub Device SDK samples](./iothub/device/samples)
* [IoT Hub Service SDK samples](./iothub/service/samples)
* [Provisioning Device SDK samples](./provisioning/device/samples)
* [Provisioning Service SDK samples](./provisioning/service/samples)

The samples require certain frameworks and SDKs to be installed on your dev machine. Please see [devbox-setup][devbox-setup] for details.

## Contribute to the Azure IoT C# SDK
If you would like to build or change the SDK source code, please follow the [devguide](doc/devguide.md).

## How to use the Azure IoT SDKs for .NET

* **Using packages and libraries**: The simplest way to use the Azure IoT SDKs is to use NuGet packages. See https://github.com/Azure/azure-iot-sdk-csharp/releases for a list released packages.

* **Build from sources**: This is for advanced users that will build and maintain their own sources. See [devbox-setup][devbox-setup] for details on how to set up your machine to build the Azure IoT SDK C#. See the End-to-end tests and azureiot.sln file for an example on how to reference the source code SDK (as opposed to NuGet packages) into your application.

## OS platforms and hardware compatibility
The IoT Hub device SDK for .NET can be used with a broad range of OS platforms and devices.

The NuGet packages provide support for the following .NET flavors:
- .NET Standard 2.0
- .NET Standard 1.3 (IoT Hub SDKs only)
- .NET Framework 4.5.1 (IoT Hub SDKs only)
- .NET MicroFramework (IoT Hub SDKs only)

For details on .NET support see the [.NET Standard documentation](https://docs.microsoft.com/en-us/dotnet/standard/net-standard).
For details on OS support see the following resources:
- [.NET Core Runtime ID Catalog](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)
- [.NET MicroFramework](http://netmf.github.io)
- [.NET Framework System Requirements](https://docs.microsoft.com/en-us/dotnet/framework/get-started/system-requirements)

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
* [Azure IoT Hub documentation][iot-hub-documentation]
* [Set up IoT Hub](doc/setup_iothub.md) describes how to configure your Azure IoT Hub service.
* [Manage IoT Hub](doc/manage_iot_hub.md) describes how to provision devices in your Azure IoT Hub service.
* [Azure Certified for IoT device catalog](https://catalog.azureiotsuite.com/)
* [Set up your development environment](./doc/devbox_setup.md) to prepare your development environment as well as how to run the samples on Linux, Windows or other platforms.
* [API reference documentation for .NET](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/devices?view=azure-dotnet)
* [Get Started with IoT Hub using .NET](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted)

# Long Term Support

The project offers a Long Term Support (LTS) version to allow users that do not need the latest features to be shielded from unwanted changes.

A new LTS version will be created every 6 months. The lifetime of an LTS branch is currently planned for one year. LTS branches receive all bug fixes that fall in one of these categories:

- security bugfixes
- critical bugfixes (crashes, memory leaks, etc.)

No new features or improvements will be picked up in an LTS branch.

LTS branches are named lts_*yyyy*_*mm*, where *mm* and *yyyy* are the month and year when the branch was created. An example of such a branch is *lts_2018_01*.

## Schedule<sup>1</sup>

Below is a table showing the mapping of the LTS branches to the packages released

| Release | Github Branch | LTS Status | LTS Start Date | Maintenance End Date | Removed Date |
| :-----------: | :-----------: | :--------: | :------------: | :------------------: | :----------: |
| [2018-1-23](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/2018-1-23) | lts_2018_01   | Active     | 2018-01-23     | 2018-06-30           | 2018-12-31   |
| [2017-10-6](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/2017-10-6) | lts_07_2017   | Deprecated     | 2017-07-01     | 2018-12-31           | 2018-06-30   |

* <sup>1</sup> All scheduled dates are subject to change by the Azure IoT SDK team.

### Planned Release Schedule
![](./lts_branches.png)

---
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

[iot-hub-documentation]: https://docs.microsoft.com/en-us/azure/iot-hub/
[iot-dev-center]: http://azure.com/iotdev
[azure-iot-sdks]: https://github.com/azure/azure-iot-sdks
[dotnet-api-reference]: https://docs.microsoft.com/en-us/dotnet/api/overview/azure/iot/client?view=azure-dotnet
[devbox-setup]: ./doc/devbox_setup.md
[get-started-dotnet]: https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted
