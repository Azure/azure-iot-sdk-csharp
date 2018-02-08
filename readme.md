# Microsoft Azure IoT SDK for .NET

This repository contains the following:
* **Microsoft Azure IoT Hub device SDK for C#** to connect client devices to Azure IoT Hub with .NET
* **Microsoft Azure IoT Hub service SDK for C#** to manage your IoT Hub service instance from a back-end .NET application

The API reference documentation for .NET SDK is [here](dotnet-api-reference).

To find SDKs in other languages for Azure IoT, please refer to the [azure-iot-sdks][azure-iot-sdks] repository.

## Developing applications for Azure IoT
Visit [Azure IoT Dev Center][iot-dev-center] to learn more about developing applications for Azure IoT.

## How to use the Azure IoT SDKs for .NET

* **Using packages and libraries**: The simplest way to use the Azure IoT SDKs is to use packages and libraries when available. Refer to [this document](./device/doc/devbox_setup.md) on how to get Azure IoT SDKs for .NET using Nuget and build applications.
* **Clone the repository**: The repository is using [GitHub Submodules](https://git-scm.com/book/en/v2/Git-Tools-Submodules) for its dependencies. In order to automatically clone these submodules, you need to use the --recursive option as described here:
```
git clone --recursive https://github.com/Azure/azure-iot-sdk-csharp.git
```

## Key features and roadmap

### Device client SDK
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


### Service client SDK
:heavy_check_mark: feature available  :heavy_multiplication_x: feature planned but not supported  :heavy_minus_sign: no support planned

| Features                                                                                                      | C# .Net             | Description                                                                                                              |
|---------------------------------------------------------------------------------------------------------------|---------------------|--------------------------------------------------------------------------------------------------------------------------|
| [Identity registry (CRUD)](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-identity-registry) | :heavy_check_mark:* | Use your backend app to perform CRUD operation for individual device or in bulk.                                         |
| [Cloud-to-device messaging](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-c2d)     | :heavy_check_mark:  | Use your backend app to send cloud-to-device messages in AMQP and AMQP-WS, and set up cloud-to-device message receivers. |
| [Direct Methods operations](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods)   | :heavy_check_mark:  | Use your backend app to invoke direct method on device.                                                                  |
| [Device Twins operations](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-device-twins)       | :x:                 | Use your backend app to perform twin operations.  This SDK only supports Get Twin at the moment.                         |
| [Query](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-query-language)                       | :heavy_check_mark:  | Use your backend app to perform query for information.                                                                   |
| [Jobs](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-jobs)                                  | :heavy_check_mark:  | Use your backend app to perform job operation.                                                                           |
| [File Upload](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-file-upload)                    | :heavy_check_mark:  | Set up your backend app to send file upload notification receiver.                                                       |

### Provisioning client SDK
This repository contains [provisioning device client SDK](./provisioning/device) for the [Device Provisioning Service](https://docs.microsoft.com/en-us/azure/iot-dps/).

:heavy_check_mark: feature available  :heavy_multiplication_x: feature planned but not supported  :heavy_minus_sign: no support planned

| Features                    | mqtt               | mqtt-ws            | amqp               | amqp-ws            | https              | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
|-----------------------------|--------------------|--------------------|--------------------|--------------------|--------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| TPM Individual Enrollment   | :heavy_minus_sign: | :heavy_minus_sign: | :heavy_check_mark: | :heavy_multiplication_x: | :heavy_check_mark: | This SDK supports connecting your device to the Device Provisioning Service via [individual enrollment](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-service#enrollment) using [Trusted Platform Module](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-security#trusted-platform-module-tpm).  Please review the [samples](./provisioning/device/samples/) folder and this [quickstart](https://docs.microsoft.com/en-us/azure/iot-dps/quick-create-simulated-device-tpm-csharp) on how to create a device client.  TPM over MQTT is currently not supported by the Device Provisioning Service.                                                                                                                                                                                                     |
| X.509 Individual Enrollment | :heavy_check_mark: |:heavy_multiplication_x: | :heavy_check_mark: | :heavy_multiplication_x: | :heavy_check_mark: | This SDK supports connecting your device to the Device Provisioning Service via [individual enrollment](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-service#enrollment) using [X.509 root certificate](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-security#root-certificate).  Please review the [samples](./provisioning/device/samples/) and this [quickstart](https://docs.microsoft.com/en-us/azure/iot-dps/quick-create-simulated-device-x509-csharp) folder on how to create a device client.   |
| X.509 Enrollment Group      | :heavy_check_mark: | :heavy_multiplication_x: | :heavy_check_mark: | :heavy_multiplication_x: | :heavy_check_mark: | This SDK supports connecting your device to the Device Provisioning Service via [individual enrollment](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-service#enrollment) using [X.509 leaf certificate](https://docs.microsoft.com/en-us/azure/iot-dps/concepts-security#leaf-certificate).  Please review the [samples](./provisioning/device/samples/) folder on how to create a device client.                                                                                                                                                                                            |

### Provisioniong service client SDK
This repository contains [provisioning service client SDK](./provisioning/service/) for the Device Provisioning Service to [programmatically enroll devices](https://docs.microsoft.com/en-us/azure/iot-dps/how-to-manage-enrollments-sdks).

| Feature                                            | Support            | Description                                                                                                                                                                                                                                            |
|----------------------------------------------------|--------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CRUD Operation with TPM Individual Enrollment      | :heavy_check_mark: | Programmatically manage device enrollment using TPM with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature. |
| Bulk CRUD Operation with TPM Individual Enrollment | :heavy_check_mark: | Programmatically bulk manage device enrollment using TPM with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature. |
| CRUD Operation with X.509 Individual Enrollment    | :heavy_check_mark: | Programmatically manage device enrollment using X.509 individual enrollment with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature. |
| CRUD Operation with X.509 Group Enrollment         | :heavy_check_mark: | Programmatically manage device enrollment using X.509 group enrollment with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature. |
| Query enrollments                                  | :heavy_check_mark: | Programmatically query registration states with the service SDK.  Please visit the [samples folder](./provisioning/service/samples/) to learn more about this feature.                                                                            |

## Samples
In the repository, you will find a set of simple samples that will help you get started:
* [Device SDK samples](./device/samples)
* [Service SDK samples](./service/Samples)

## OS platforms and hardware compatibility
The IoT Hub device SDK for .NET can be used with a broad range of OS platforms and devices.

Minimum requirements:
- .NET platform support (see https://dot.net for supported platforms)
- Device SDK: .NET Framework 4.5; .NET Standard 1.3; PCL

## Contribution, feedback and issues

If you encounter any bugs, have suggestions for new features or if you would like to become an active contributor to this project please follow the instructions provided in the [contribution guidelines](.github/CONTRIBUTING.md).

For suggestions regarding the Azure IoT Services, please use https://feedback.azure.com/forums/321918-azure-iot

## Support

If you are having issues using one of the packages or using the Azure IoT Hub service that go beyond simple bug fixes or help requests that would be dealt within the issues section of this project, the Microsoft Customer Support team will try and help out on a best effort basis.
To engage Microsoft support, you can create a support ticket directly from the [Azure portal](https://ms.portal.azure.com/#blade/Microsoft_Azure_Support/HelpAndSupportBlade).
Escalated support requests for Azure IoT Hub SDKs development questions will only be available Monday thru Friday during normal coverage hours of 6 a.m. to 6 p.m. PST.
Here is what you can expect Microsoft Support to be able to help with:
- **SDKs issues**: If you are trying to compile and run the libraries on a supported platform, the Support team will be able to assist with troubleshooting or questions related to compiler issues and communications to and from the IoT Hub.  They will also try to assist with questions related to porting to an unsupported platform, but will be limited in how much assistance can be provided.  The team will be limited with trouble-shooting the hardware device itself or drivers and or specific properties on that device. 
- **IoT Hub / Connectivity Issues**: Communication from the device client to the Azure IoT Hub service and communication from the Azure IoT Hub service to the client.  Or any other issues specifically related to the Azure IoT Hub.
- **Portal Issues**: Issues related to the portal, that includes access, security, dashboard, devices, Alarms, Usage, Settings and Actions.
- **REST/API Issues**: Using the IoT Hub REST/APIs that are documented in the [documentation](https://docs.microsoft.com/en-us/rest/api/iothub/).

## Read more
* [Azure IoT Hub documentation][iot-hub-documentation]
* [Set up IoT Hub](doc/setup_iothub.md) describes how to configure your Azure IoT Hub service.
* [Manage IoT Hub](doc/manage_iot_hub.md) describes how to provision devices in your Azure IoT Hub service.
* [FAQ](doc/faq.md) contains frequently asked questions about the SDKs and libraries.
* [Azure Certified for IoT device catalog](https://catalog.azureiotsuite.com/)
* [Set up your development environment](./device/doc/devbox_setup.md) to prepare your development environment as well as how to run the samples on Linux, Windows or other platforms.
* [API reference documentation for .NET](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/devices?view=azure-dotnet)
* [Get Started with IoT Hub using .NET](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted)

## SDK folder structure

### /device

Contains Azure IoT Hub client components that provide the raw messaging capabilities of the library. Refer to the API documentation and samples for information on how to use it.

### /doc

Contains application development guides and device setup instructions.

### /e2e

Contains end-to-end tests.

### /jenkins

Contains scripts used by the Continuous Integration system (Jenkins).

### /provisioning

Contains the Azure IoT Provisioning client components.

### /security

Contains the SecurityProvider components for Hardware Security Modules.

### /service

Contains libraries that enable interactions with the IoT Hub service to perform operations such as sending messages to devices and managing the device identity registry.

### /shared

Contains source code for the Microsoft.Azure.Devices.Shared component. This component contains public API shared across the SDK.

### /tools/DeviceExplorer

Contains the source code for the Azure IoT Device Explorer tool.

### /transport

Contains the TransportClient components.

# Long Term Support

The project offers a Long Term Support (LTS) version to allow users that do not need the latest features to be shielded from unwanted changes.

A new LTS version will be created every 6 months. The lifetime of an LTS branch is currently planned for one year. LTS branches receive all bug fixes that fall in one of these categories:

- security bugfixes
- critical bugfixes (crashes, memory leaks, etc.)

No new features or improvements will be picked up in an LTS branch.

LTS branches are named lts_*mm*_*yyyy*, where *mm* and *yyyy* are the month and year when the branch was created. An example of such a branch is *lts_07_2017*.

## Schedule<sup>1</sup>

Below is a table showing the mapping of the LTS branches to the packages released

| Nuget Package | Github Branch | LTS Status | LTS Start Date | Maintenance End Date | Removed Date |
| :-----------: | :-----------: | :--------: | :------------: | :------------------: | :----------: |
| Microsoft.Azure.Devices.Client v1.5.1, Microsoft.Azure.Devices v1.4.0, Microsoft.Azure.Devices.Shared v1.1.1 | lts_07_2017   | Active     | 2017-07-01     | 2017-12-31           | 2018-06-30   |

* <sup>1</sup> All scheduled dates are subject to change by the Azure IoT SDK team.

### Planned Release Schedule
![](./lts_branches.png)

---
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

[iot-hub-documentation]: https://docs.microsoft.com/en-us/azure/iot-hub/
[iot-dev-center]: http://azure.com/iotdev
[azure-iot-sdks]: https://github.com/azure/azure-iot-sdks
[dotnet-api-reference]: https://docs.microsoft.com/en-us/dotnet/api/overview/azure/iot?view=azure-dotnet
[devbox-setup]: ./device/doc/devbox_setup.md
[get-started-dotnet]: https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted
