
This folder contains simple samples showing how to use the various features of Microsoft Azure IoT Hub service, from a device running C# code.

### Device samples

- [Reconnection][d-message-sample] sample
    - This sample illustrates how to write a device application to handle connection issues, connection-related exceptions, and how to manage the lifetime of the `DeviceClient`
    - Includes sending messages and symmetric key failover
- [Method][d-method-sample] sample
- [Edge module method][d-edge-module-method-sample] sample
- [Receive message][d-receive-message-sample] sample
- [Edge module message][d-edge-module-message-sample] sample
- [Twin][d-twin-sample] sample
- [File upload][d-file-upload-sample] sample
- [Connect with X.509 certificate][d-x509-cert-sample] sample
- [Plug and Play samples][d-pnp-sample] sample
- [Xamarin][d-xamarin-sample] sample
- [Simulated device][d-simulated-device-sample] sample
- [Simulated device with command][d-simulated-device-with-command-sample] sample
- [Hub routing][d-hub-routing-sample] sample

### Prerequisites

In order to run the device samples on Linux or Windows, you will first need the following prerequisites:

- [Setup your IoT hub][lnk-setup-iot-hub]
- [Provision your device and get its credentials][lnk-manage-iot-device]
- [Setup your IoT Edge device and Edge module][lnk-setup-iot-edge] (Only for IoT edge samples)

### Setup environment

The following prerequisite is the minimum requirement to build and run the samples. 

- Install the latest .NET Core from <https://dot.net>

> Visual Studio is **not required** to run the samples.

### Get and run the samples

You need to clone the repository or download the sample (the one you want to try) project's folder on your device.

#### Build and run the samples

1. Building the sample application

    To build the sample application using dotnet, from terminal navigate to the sample folder (where the .csproj file lives). Then execute the following command and check for build errors:

    ```console
    dotnet build
    ```

1. Preparing the sample application:
   1. Many of these samples take parameters. To see the parameters required, type:

      ```console
      dotnet run --help
      ```

1. Running the sample application:

    To run the sample application using dotnet, execute the following command with any required parameters discovered in the previous step.

    ```console
    dotnet run
    ```

[d-message-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/v2/iothub/device/samples/how%20to%20guides/DeviceReconnectionSample
[d-edge-module-message-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/v2/iothub/device/samples/getting%20started/EdgeModuleMessageSample
[d-receive-message-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/v2/iothub/device/samples/getting%20started/MessageReceiveSample
[d-method-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/v2/iothub/device/samples/getting%20started/MethodSample
[d-edge-module-method-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/v2/iothub/device/samples/getting%20started/EdgeModuleMethodSample 
[d-twin-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/v2/iothub/device/samples/getting%20started/TwinSample
[d-file-upload-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/v2/iothub/device/samples/getting%20started/FileUploadSample
[d-x509-cert-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/v2/iothub/device/samples/how%20to%20guides/X509DeviceCertWithChainSample
[d-pnp-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/v2/iothub/device/samples/solutions/PnpDeviceSamples
[d-xamarin-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/v2/iothub/device/samples/how%20to%20guides/XamarinSample
[lnk-setup-iot-hub]: https://aka.ms/howtocreateazureiothub
[lnk-manage-iot-device]: https://github.com/Azure/azure-iot-device-ecosystem/blob/master/setup_iothub.md#create-new-device-in-the-iot-hub-device-identity-registry
[lnk-setup-iot-edge]: https://github.com/Azure/azure-iot-sdk-csharp/blob/previews/v2/iothub/device/samples/getting%20started/EdgeModuleMessageSample/ReadMe.md
[d-simulated-device-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/v2/iothub/device/samples/getting%20started/SimulatedDevice
[d-simulated-device-with-command-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/v2/iothub/device/samples/getting%20started/SimulatedDeviceWithCommand
[d-hub-routing-sample]: https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/v2/iothub/device/samples/how%20to%20guides/HubRoutingSample