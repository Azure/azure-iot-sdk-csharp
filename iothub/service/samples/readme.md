## Service samples for Azure IoT Service SDK for C#

Here, you'll find simple samples showing how to use the various features of Microsoft Azure IoT Hub service from a back-end application running C# code.

### List of Samples

* [ADM Sample][adm-sample]
* [Device Streaming Sample][device-streaming-sample]
* [Edge Deployment Sample][edge-deployment-sample]
* [Import Export Devices Sample][import-export-sample]
* [Jobs Sample][jobs-sample]
* [Registry ManagerSample][reg-man-sample]
* [Service Client Sample][service-client-sample]
* [Plug and play Samples ][pnp-service-sample]

### Prerequisites
In order to run the device samples on Linux or Windows, you will first need the following prerequisites:
* [Setup your IoT hub][lnk-setup-iot-hub].
* After creating the IoT Hub, [retrieve the iothubowner connection string][lnl-credentials].

### Setup environment
Find development setup and prerequistes [here][lnk-prereq].

### Get and run the samples
You need to first clone the repository or download the sample project folder to your machine.

### Plug and Play Samples

Shows how to get the plug and play model ID for a device, update properties on the twin and invoke commands on a device.

#### Build and run the Pnp(plug and play) Service Sample application for a device with no components:
This sample uses the following simple model, which has no components - [Thermostat](https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/samples/Thermostat.json)
1. Preparing the Pnp service Sample application:
   1. Set the following environment variables on the terminal from which you want to run the application.

      * IOTHUB_CONNECTION_STRING
      * DEVICE_ID

2. Building the Pnp Service Sample application:

    To build the Pnp Service Sample application using dotnet, from terminal navigate to the **\service\samples\PnpServiceSamples\Thermostat** folder. Then execute the following command and check for build errors:

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

    To build the Pnp Service Sample application using dotnet, from terminal navigate to the **\service\samples\PnpServiceSamples\TemperatureController** folder. Then execute the following command and check for build errors:

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
[device-streaming-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/service/DeviceStreamingSample
[edge-deployment-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/service/EdgeDeploymentSample
[import-export-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/service/ImportExportDevicesSample
[jobs-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/service/JobsSample
[reg-man-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/service/RegistryManagerSample
[service-client-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/service/ServiceClientSample
[pnp-service-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/preview/iothub/service/samples/PnpServiceSamples
[lnk-setup-iot-hub]: https://aka.ms/howtocreateazureiothub
[lnl-credentials]: https://github.com/Azure/azure-iot-device-ecosystem/blob/master/setup_iothub.md#retrieving-user-credentials-to-interact-with-the-service-not-as-a-device
[lnk-prereq]: https://github.com/Azure-Samples/azure-iot-samples-csharp#prerequisites