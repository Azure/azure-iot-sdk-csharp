# Microsoft Azure IoT device SDK for .NET

This folder contains the following:
* The Azure IoT device SDK for .NET to easily and securely connect devices to the Microsoft Azure IoT Hub service.
* Samples showing how to use the SDK

The library is available as a NuGet package for you include in your own development projects. This repository contains documentation and samples to help you get started using this SDK.

## Features

 * Sends event data to Azure IoT based services.
 * Maps server commands to device functions.
 * Batches messages to improve communication efficiency.
 * Supports pluggable transport protocols.

> Note: Currently, **Microsoft.Azure.Devices.Client.WinRT** doesn't support the **MQTT** protocol.

For example,calling `DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt);` will result in "Mqtt protocol is not supported" exception.

> Note : Currently, **Microsoft.Azure.Devices.Client.PCL** only supports the **HTTPS** protocol.

For example, calling `DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Amqp);` will result in "Amqp protocol is not supported" exception.

## How to use the Azure IoT SDKs for .NET

* **Using packages and libraries**: the simplest and recommended way to use the Azure IoT SDKs is to use packages and libraries when available. The following are available, based on your application:
    * .NET and UWP apps (C#, C++, JS)    : [Microsoft.Azure.Devices.Client](./doc/devbox_setup.md#directly_using_sdk)
    * Xamarin (iOS and Android)          : [Microsoft.Azure.Devices.Client.PCL](./doc/devbox_setup.md#directly_using_sdk)
*  **Compiling the source code**: when no package or library is available for your platform or if you want to modify the SDKs code, or port the SDKs to a new platform, then you can leverage the build environement provided in the repository.
    * [Building the .NET SDK](./doc/devbox_setup.md#building_sdk)

## Application development guidelines
For more information on how to use this library refer to the documents below:
- [Preparing your Windows development environment][devbox-setup]
- [Running the samples on Windows][run-sample-on-desktop-windows]
- [Running the samples on Windows IoT Core][run-sample-on-windows-iot-core]
- [Running a Xamarin application][run-csharp-pcl]

Other useful documents include:
- [Setup IoT Hub][setup-iothub]
- [Microsoft Azure IoT device SDK for .NET API reference][dotnet-api-ref]

## Samples

The repository contains a set of simple samples that will help you get started.
You can find a list of these samples with instructions on how to run them [here](samples/readme.md). 

## Folder structure of repository

All the .NET device specific resources are located in the **csharp** folder.

### /build

This folder contains build scripts for the .NET client libraries and samples.

### /doc

This folder contains setup and getting started documents for .NET.

### /Microsoft.Azure.Devices.Client  /Microsoft.Azure.Devices.Client.WinRT

These folders contain the .NET client library source code.

The Microsoft.Azure.Devices.Client.WinRT project is for building the UWP (Universal Windows Platform) version of the client library. For more information about the UWP version of this library refer to the [FAQ][faq-doc].

These projects are useful if you want to modify or extend the .NET libraries.

### /NuGet

This folder contains scripts for building the NuGet package that contains the library components.

### /samples

This folder contains various .NET samples that illustrate how to use the client library.

### /iothub_csharp_client.sln

This Visual Studio solution contains the client library and sample projects.

## API reference

API reference documentation can be found online at https://msdn.microsoft.com/library/microsoft.azure.devices.aspx.

[setup-iothub]: ../../doc/setup_iothub.md
[devbox-setup]: doc/devbox_setup.md
[run-sample-on-desktop-windows]: doc/windows-desktop-csharp.md
[run-sample-on-windows-iot-core]: doc/windows10-iotcore-csharp.md
[run-csharp-pcl]: doc/csharp-pcl.md
[dotnet-api-ref]: https://msdn.microsoft.com/library/microsoft.azure.devices.aspx
