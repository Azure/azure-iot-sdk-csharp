# Azure IoT Digital Twins Device SDK Sample

**PREVIEW - WILL LIKELY HAVE BREAKING CHANGES**

This project contains a sample implementation of a simulated environmental sensor. It shows how to:

  * Implement the environmental sensor interface
  * Create an interfaceInstance for this interface
  * Use the digital twin device client to register this interfaceInstance and interact with the Digital Twins services.
  * How to respond to command invocations
  * How to respond to property updates

## How to run the sample

### Setup environment

This sample is a .NET Core 2.2 project, so to build and run this sample, 
your device must have the .NET Core 2.2 SDK installed. 

[Follow this link to download this SDK][netcore-sdk-download]

### Sample Arguments

In order to run this sample, you must set environment variables for:
- "DIGITAL_TWIN_DEVICE_CONNECTION_STRING" : Your IoT Hub device's connection string

### Other Prerequisites
In order to run this sample, you will need an IoT Hub. You will also need at least one device registered in this hub so that the sample can register as that device
* [Setup Your IoT Hub][lnk-setup-iot-hub]

### Run the sample

From this folder, run the following command:

```sh
dotnet run
```

This will build the necessary Nuget packages and run the sample for you

The sample will register to use the Environmental Sensor interface, report some properties on the interface, send some telemetry on the
interface, and then will sit idle and wait for updates from the cloud such as command invocations and writable property updates

[netcore-sdk-download]: https://dotnet.microsoft.com/download/dotnet-core/2.2
[lnk-setup-iot-hub]: https://aka.ms/howtocreateazureiothub
