# Digital Twin Client Samples for the Azure IoT Service SDK
This folder contains samples to illustrate how to use the DigitalTwinClient to perform server-side operations on plug-and-play compatible devices.

## List of Samples
* [Thermostat Sample][lnk-thermostat-sample]: Perform root-level operations on a plug and play compatible device.
* [Temperature Controller Sample][lnk-temperatureController-sample]: Perform component-level operations on a plug and play compatible device.

## Prerequisites
In order to run the device samples on Linux or Windows, you will first need the following prerequisites:
* [Set up your IoT hub][lnk-setup-iot-hub]
* [Provision your device and get its credentials][lnk-manage-iot-device]

## Setup Environment

Visual Studio is **not required** to run the samples.

- Install the latest .NET Core from https://dot.net

## Get the samples
You need to clone the repository or download the sample (the one you want to try) project's folder on your device.

## Build and run the samples

> Please note that the service side samples here require that the device side samples associated with the service side samples to be running in parrallel. For instance, you need to run the [Thermostat][lnk-thermostat-device-sample] device sample before your run the [Thermostat][lnk-thermostat-sample] service side sample. And the same goes for the TempreatureController sample; you need to run the [TemperatureController][lnk-temperaturecontroller-device-sample] device sample before you run the [TemperatureController][lnk-temperatureController-sample] service side sample.

1. Set the following environment variables on the terminal from which you want to run the application.
    * IOTHUB_CONNECTION_STRING
    * IOTHUB_DEVICE_ID
2. To build the sample application using dotnet, from terminal navigate to the sample folder (where the .csproj file lives). Then execute the following command and check for build errors:
    ```
    dotnet build
    ```

3. To run the sample application using dotnet, execute the following command.
    ```
    dotnet run
    ```

[lnk-thermostat-sample]: /iothub/service/samples/solutions/DigitalTwinClientSamples/Thermostat

[lnk-temperatureController-sample]: /iothub/service/samples/solutions/DigitalTwinClientSamples/TemperatureController

[lnk-setup-iot-hub]: https://aka.ms/howtocreateazureiothub

[lnk-manage-iot-device]: https://github.com/Azure/azure-iot-device-ecosystem/blob/master/setup_iothub.md#create-new-device-in-the-iot-hub-device-identity-registry

[lnk-thermostat-device-sample]: /iothub/device/samples/solutions/PnpDeviceSamples/Thermostat

[lnk-temperaturecontroller-device-sample]: /iothub/device/samples/solutions/PnpDeviceSamples/TemperatureController

