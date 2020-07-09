
This folder contains simple samples showing how to use the various features of Microsoft Azure IoT Hub service, from a device running C# code.

### [Device samples][device-samples]
* [Message Sample][d-message-sample]
* [Method Sample][d-method-sample]
* [Twin Sample][d-twin-sample]
* [File Upload Sample][d-file-upload-sample]
* [Keys Rollover Sample][d-keys-rollover-sample]
* [Import Export Devices Sample][d-import-export-devices-sample]
* [Plug and Play Device Sample][d-pnp-sample]
* [Xamarin Sample][d-xamarin-sample]

### [Module samples][module-samples]
* [Message Sample][m-message-sample]
* [Twin Sample][m-twin-sample]

### Prerequisites
In order to run the device samples on Linux or Windows, you will first need the following prerequisites:
* [Setup your IoT hub][lnk-setup-iot-hub]
* [Provision your device and get its credentials][lnk-manage-iot-device]

### Setup environment

The following prerequisite is the minimum requirement to build and run the samples. 

Visual Studio is **not required** to run the samples.

- Install the latest .NET Core from https://dot.net

### Get and run the samples
You need to clone the repository or download the sample (the one you want to try) project's folder on your device.

#### Build and run the samples:
1. Preparing the sample application:
   1. Set the following environment variables on the terminal from which you want to run the application.

      * IOTHUB_DEVICE_CONNECTION_STRING

2. Building the sample application:

    To build the sample application using dotnet, from terminal navigate to the sample folder (where the .csproj file lives). Then execute the following command and check for build errors:

    ```
    dotnet build
    ```

3. Running the sample application:

	To run the sample application using dotnet, execute the following command.

    ```
    dotnet run
    ```

[device-samples]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device
[d-message-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/MessageSample
[d-method-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/MethodSample
[d-twin-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/TwinSample
[d-file-upload-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/FileUploadSample
[d-keys-rollover-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/KeysRolloverSample
[d-import-export-devices-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/ImportExportDevicesSample
[d-pnp-sample]: /iothub/device/samples/PnpDeviceSamples
[d-xamarin-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/XamarinSample

[module-samples]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/module
[m-message-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/module/MessageSample
[m-twin-sample]: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/module/TwinSample

[lnk-setup-iot-hub]: https://aka.ms/howtocreateazureiothub
[lnk-manage-iot-device]: https://github.com/Azure/azure-iot-device-ecosystem/blob/master/setup_iothub.md#create-new-device-in-the-iot-hub-device-identity-registry