# Microsoft Azure IoT SDK for .NET

This repository contains the following:
* **Azure IoT Hub Device SDK for .NET**: to connect client devices to Azure IoT Hub
* **Azure IoT Hub Service SDK for .NET**: enables developing back-end applications for Azure IoT

To find SDKs in other languages for Azure IoT, please refer to the [azure-iot-sdks][azure-iot-sdks] repository.

## Developing applications for Azure IoT

Visit [Azure IoT Dev Center][iot-dev-center] to learn more about developing applications for Azure IoT.

## How to use the Azure IoT SDKs for .NET 

Devices and data sources in an IoT solution can range from a simple network-connected sensor to a powerful, standalone computing device. Devices may have limited processing capability, memory, communication bandwidth, and communication protocol support. The IoT device SDKs enable you to implement client applications for a wide variety of devices. The API reference documentation is [here][dotnet-api-reference].

* **Using nuget packages**: the simplest way to use the Azure IoT SDKs for .NET to develop device apps is to leverage the [nuget](https://www.nuget.or) packages:
   * [Device SDK](./device/readme.md)
   * [Service SDK](./service/readme.md)

* **Working with the SDKs code**: if you are working with the SDKs code to modify it or to contribute changes, then you can clone the repository and build the libraries following [these instructions](.device/devbox-setup.md).

## Samples

Whithin the repository, you can find various types of simple samples that can help you get started.
- [Device SDK samples](./device/samples/readme.md)
- [Service SDK samples](./service/samples/readme.md)

## Contribution, feedback and issues

If you encounter any bugs, have suggestions for new features or if you would like to become an active contributor to this project please follow the instructions provided in the [contribution guidelines](CONTRIBUTING.md).

## Support

If you are having issues using one of the packages or using the Azure IoT Hub service that go beyond simple bug fixes or help requests that would be dealt within the [issues section](./issues) of this project, the Microsoft Customer Support team will try and help out on a best effort basis.
To engage Microsoft support, you can create a support ticket directly from the [Azure portal](https://ms.portal.azure.com/#blade/Microsoft_Azure_Support/HelpAndSupportBlade).
Escalated support requests for Azure IoT Hub SDKs development questions will only be available Monday thru Friday during normal coverage hours of 6 a.m. to 6 p.m. PST.
Here is what you can expect Microsoft Support to be able to help with:
* **Client SDKs issues**: If you are trying to compile and run the libraries on a supported platform, the Support team will be able to assist with troubleshooting or questions related to compiler issues and communications to and from the IoT Hub.  They will also try to assist with questions related to porting to an unsupported platform, but will be limited in how much assistance can be provided.  The team will be limited with trouble-shooting the hardware device itself or drivers and or specific properties on that device. 
* **IoT Hub / Connectivity Issues**: Communication from the device client to the Azure IoT Hub service and communication from the Azure IoT Hub service to the client.  Or any other issues specifically related to the Azure IoT Hub.
* **Portal Issues**: Issues related to the portal, that includes access, security, dashboard, devices, Alarms, Usage, Settings and Actions.
* **REST/API Issues**: Using the IoT Hub REST/APIs that are documented in the [documentation]( https://msdn.microsoft.com/library/mt548492.aspx).

## Read more

* [Azure IoT Hub documentation][iot-hub-documentation]
* [.NET API reference documentation][dotnet-api-reference]

---

## SDK folder structure

### /device

This folder contains the source code, samples and documentation for the .NET Device SDK.

### /doc

This folder contains application development guides and device setup instructions.

### /service

This folder contains the source code, samples and documentation for the .NET Service SDK.

---
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

[iot-dev-center]: http://azure.com/iotdev
[iot-hub-documentation]: https://docs.microsoft.com/en-us/azure/iot-hub/
[dotnet-api-reference]: http://azure.github.io/azure-iot-sdks/
