# Get Model Sample

This sample demonstrates how to retrieve a model definition from the model repo

## How to run the sample

### Setup environment

This sample is a .NET Core 2.2 project, so to build and run this sample, 
your device must have the .NET Core 2.2 SDK installed. 

[Follow this link to download this SDK][netcore-sdk-download]

### Sample Arguments

In order to run this sample, you must set environment variables for:
- "IOTHUB_CONNECTION_STRING" : Your IoT Hub's connection string
- "MODEL_ID" : Your model id to look up the full definition for

### Other Prerequisites
In order to run this sample, you will need an IoT Hub.
* [Setup Your IoT Hub][lnk-setup-iot-hub]

### Run the sample

This sample can be run using either Visual Studio 2019 or from command line

To run this sample from commandline, run the following command from this folder:

```sh
dotnet run
```

This will build the necessary Nuget packages and run the sample for you

The sample will print out the model definition that belongs to the provided model id

```sh
Successfully retrieved the model, the definition is:

{
  "@id": "urn:azureiot:DeviceManagement:DeviceInformation:1",
  "@type": "Interface",
  "displayName": "Device Information",
  "contents": [
    {
      "@type": "Property",
      "name": "manufacturer",
      "displayName": "Manufacturer",
      "schema": "string",
      "description": "Company name of the device manufacturer. This could be the same as the name of the original equipment manufacturer (OEM). Ex. Contoso."
    },
    {
      "@type": "Property",
      "name": "model",
      "displayName": "Device model",
      "schema": "string",
      "description": "Device model name or ID. Ex. Surface Book 2."
    },
    {
      "@type": "Property",
      "name": "swVersion",
      "displayName": "Software version",
      "schema": "string",
      "description": "Version of the software on your device. This could be the version of your firmware. Ex. 1.3.45"
    },
    {
      "@type": "Property",
      "name": "osName",
      "displayName": "Operating system name",
      "schema": "string",
      "description": "Name of the operating system on the device. Ex. Windows 10 IoT Core."
    },
    {
      "@type": "Property",
      "name": "processorArchitecture",
      "displayName": "Processor architecture",
      "schema": "string",
      "description": "Architecture of the processor on the device. Ex. x64 or ARM."
    },
    {
      "@type": "Property",
      "name": "processorManufacturer",
      "displayName": "Processor manufacturer",
      "schema": "string",
      "description": "Name of the manufacturer of the processor on the device. Ex. Intel."
    },
    {
      "@type": "Property",
      "name": "totalStorage",
      "displayName": "Total storage",
      "schema": "long",
      "displayUnit": "kilobytes",
      "description": "Total available storage on the device in kilobytes. Ex. 2048000 kilobytes."
    },
    {
      "@type": "Property",
      "name": "totalMemory",
      "displayName": "Total memory",
      "schema": "long",
      "displayUnit": "kilobytes",
      "description": "Total available memory on the device in kilobytes. Ex. 256000 kilobytes."
    }
  ],
  "@context": "http://azureiot.com/v1/contexts/IoTModel.json"
}
```

[lnk-setup-iot-hub]: https://aka.ms/howtocreateazureiothub
[netcore-sdk-download]: https://dotnet.microsoft.com/download/dotnet-core/2.2
