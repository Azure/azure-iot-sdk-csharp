# Get Digital Twin Sample

This sample code demonstrates how to check the state of a single digital twin.

## How to run the sample

### Setup environment

This sample is a .NET Core 2.2 project, so to build and run this sample, 
your device must have the .NET Core 2.2 SDK installed. 

[Follow this link to download this SDK][netcore-sdk-download]

### Sample Arguments

In order to run this sample, you must set environment variables for:
- "IOTHUB_CONNECTION_STRING" : Your IoT Hub's connection string
- "DEVICE_ID" : The ID of the device to invoke the command onto

### Other Prerequisites
In order to run this sample, you will need an IoT Hub. You will also need at least one device registered in this hub so that the sample can interact with that device
* [Setup Your IoT Hub][lnk-setup-iot-hub]

### Run the sample

This sample can be run using either Visual Studio 2019 or from command line

To run this sample from commandline, run the following command from this folder:

```sh
dotnet run
```

This will build the necessary Nuget packages and run the sample for you

The sample will print out the state of the digital twin that was queried and then exit.

```sh
Got the status of the digital twin successfully, the returned string was:
{
  "interfaces": {
    "urn_azureiot_ModelDiscovery_DigitalTwin": {
      "name": "urn_azureiot_ModelDiscovery_DigitalTwin",
      "properties": {
        "modelInformation": {
          "reported": {
            "value": {
              "interfaces": {
                "urn_azureiot_ModelDiscovery_DigitalTwin": "urn:azureiot:ModelDiscovery:DigitalTwin:1"
              }
            }
          }
        }
      }
    }
  },
  "version": 1
}
```

[lnk-setup-iot-hub]: https://aka.ms/howtocreateazureiothub
[netcore-sdk-download]: https://dotnet.microsoft.com/download/dotnet-core/2.2
