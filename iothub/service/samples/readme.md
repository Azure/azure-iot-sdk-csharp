## Service samples for Azure IoT Service SDK for C#

Here, you'll find simple samples showing how to use the various features of the Microsoft Azure IoT Hub service from a back-end application running C# code.

### List of Samples

* [ADM Sample][adm-sample]
* [Jobs Sample][jobs-sample]
* [Registry ManagerSample][reg-man-sample]
* [Service Client Sample][service-client-sample]
* [Plug and play Samples ][pnp-service-sample]

### Prerequisites
In order to run the device samples on Linux or Windows, you will first need the following prerequisites:
* [Setup your IoT hub][lnk-setup-iot-hub]
* After creating the IoT Hub, retreive the iothubowner connection string (mentioned in instructions to create the hub).

### Setup environment
.NET Core SDK 3.0.0 or greater on your development machine. You can download the .NET Core SDK for multiple platforms from .NET. You can verify the current version of C# on your development machine using 'dotnet --version'.

Note: The samples can be compiled using the NET Core SDK 2.1 SDK if the language version of projects using C# 8 features are changed to preview.

### Get and run the samples
You need to first clone the repository or download the sample project folder on your machine.

### Plug and Play Samples

Shows how to get the plug and play model ID, update properties on the twin and invoke commands.

#### Build and run the Pnp(plug and play) Service Sample application for a device with no components:
This sample uses the following simple model which has no components - [Thermostat](https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/samples/Thermostat.json)
1. Preparing the Pnp service Sample application:
   1. Set the following environment variables on the terminal from which you want to run the application.

      * IOTHUB_CONNECTION_STRING
      * DEVICE_ID

2. Building the Pnp Service Sample application:

    To build the Pnp Service Sample application using dotnet, at a command prompt navigate to the **\service\samples\PnpServiceSamples\Thermostat** folder. Then execute the following command and check for build errors:

    ```
    dotnet build
    ```

3. Running the Pnp Service Sample application:

	To run the Pnp Service Sample application using dotnet, execute the following command.

    ```
    dotnet run
    ```


#### Build and run the Pnp(plug and play) Service Sample application for a device with components:
This sample uses the following model which has components - [TemperatureController](https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/samples/TemperatureController.json)
1. Preparing the Pnp service Sample application:
    1. Set the following environment variables on the terminal from which you want to run the application.

        * IOTHUB_CONNECTION_STRING
        * DEVICE_ID

2. Building the Pnp Service Sample application:

    To build the Pnp Service Sample application using dotnet, at a command prompt navigate to the **\service\samples\PnpServiceSamples\TemperatureController** folder. Then execute the following command and check for build errors:

    ```
    dotnet build
    ```

3. Running the Pnp Service Sample application:

    To run the Pnp Service Sample application using dotnet, execute the following command.

    ```
    dotnet run
    ```

[samples-repo]: https://github.com/Azure-Samples/azure-iot-samples-csharp
[service-samples]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/service
[adm-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/service/AutomaticDeviceManagementSample
[jobs-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/service/JobsSample
[reg-man-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/service/RegistryManagerSample
[service-client-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/service/ServiceClientSample
[pnp-service-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/preview/iothub/service/samples/PnpServiceSamples
