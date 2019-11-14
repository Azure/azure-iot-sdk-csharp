#  Update Digital Twin Sample

This sample demonstrates how to update a single property on a single interface on a digital twin. There is also unused
code that shows how to update multiple properties on a single interface on a digital twin

## How to run the sample

### Setup environment

This sample is a .NET Core 2.2 project, so to build and run this sample, 
your device must have the .NET Core 2.2 SDK installed. 

[Follow this link to download this SDK][netcore-sdk-download]

### Sample Arguments

In order to run this sample, you must set environment variables for:
- "IOTHUB_CONNECTION_STRING" : Your IoT Hub's connection string
- "DEVICE_ID" : The ID of the device to invoke the command onto
- "INTERFACE_INSTANCE_NAME" : The interface the command belongs to
- "PROPERTY_NAME" : The name of the property to update on your digital twin
- "PROPERTY_VALUE" : The value of the property to set

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

The sample will update the provided property on the device, print out the response Json from the service, and then exit.

```sh
Property updated on the device successfully, the returned payload was
{
  "interfaces": {
    "urn_azureiot_ModelDiscovery_DigitalTwin": {
      "name": "urn_azureiot_ModelDiscovery_DigitalTwin",
      "properties": {
        "modelInformation": {
          "reported": {
            "value": {
              "modelId": "urn:contoso:azureiot:sdk:testinterface:cm:1",
              "interfaces": {
                "urn_azureiot_ModelDiscovery_ModelInformation": "urn:azureiot:ModelDiscovery:ModelInformation:1",
                "urn_azureiot_Client_SDKInformation": "urn:azureiot:Client:SDKInformation:1",
                "deviceInformation": "urn:azureiot:DeviceManagement:DeviceInformation:1",
                "testInterfaceInstanceName": "urn:contoso:azureiot:sdk:testinterface:1",
                "urn_azureiot_ModelDiscovery_DigitalTwin": "urn:azureiot:ModelDiscovery:DigitalTwin:1"
              }
            }
          }
        }
      }
    },
    "testInterfaceInstanceName": {
      "name": "testInterfaceInstanceName",
      "properties": {
        "writableProperty": {
          "desired": {
            "value": "someString"
          }
        }
      }
    },
    "urn_azureiot_Client_SDKInformation": {
      "name": "urn_azureiot_Client_SDKInformation",
      "properties": {
        "language": {
          "reported": {
            "value": "Csharp"
          }
        },
        "version": {
          "reported": {
            "value": "0.0.1"
          }
        },
        "vendor": {
          "reported": {
            "value": "Microsoft"
          }
        }
      }
    }
  },
  "version": 2
}
```

[lnk-setup-iot-hub]: https://aka.ms/howtocreateazureiothub
[netcore-sdk-download]: https://dotnet.microsoft.com/download/dotnet-core/2.2
