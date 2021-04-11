---
page_type: sample
description: "A set of samples that show how a device that uses the IoT Plug and Play conventions interacts with either IoT Hub or IoT Central."
languages:
- csharp
products:
- azure
- azure-iot-hub
- azure-iot-central
- azure-iot-pnp
- dotnet
urlFragment: azure-iot-pnp-device-samples-for-csharp-net
---

# IoT Plug And Play device samples

These samples demonstrate how a device that follows the [IoT Plug and Play conventions][pnp-convention] interacts with IoT Hub or IoT Central, to:

- Send telemetry.
- Update read-only and read-write properties.
- Respond to command invocation.

The samples demonstrate two scenarios:

- An IoT Plug and Play device that implements the [Thermostat][d-thermostat] model. This model has a single interface that defines telemetry, read-only and read-write properties, and commands.
- An IoT Plug and Play device that implements the [Temperature controller][d-temperature-controller] model. This model uses multiple components:
  - The top-level interface defines telemetry, read-only property and commands.
  - The model includes two [Thermostat][thermostat-model] components, and a [device information][d-device-info] component.

## Configuring the samples in Visual Studio

These samples use the `launchSettings.json` in Visual Studio for different configuration settings, one for direct connection strings and one for the Device Provisioning Service (DPS).

The configuration file is committed to the repository as `launchSettings.template.json`. Rename the file to `launchSettings.json` and then configure it from the **Debug** tab in the project properties.

## Configuring the samples in VSCode

These samples use the `launch.json` in Visual Studio Code for different configuration settings, one for direct connection strings and one for DPS.

The configuration file is committed to the repository as `launch.template.json`. Rename it to `launch.json` to take effect when you start a debugging session.

## Quickstarts and tutorials

To learn more about how to configure and run the Thermostat device sample with IoT Hub, see [Quickstart: Connect a sample IoT Plug and Play device application running on Linux or Windows to IoT Hub][thermostat-hub-qs].

To learn more about how to configure and run the Temperature Controller device sample with:

- IoT Hub, see [Tutorial: Connect an IoT Plug and Play multiple component device application running on Linux or Windows to IoT Hub][temp-controller-hub-tutorial]
- IoT Central, see [Tutorial: Create and connect a client application to your Azure IoT Central application][temp-controller-central-tutorial]

[pnp-convention]: https://docs.microsoft.com/azure/iot-pnp/concepts-convention
[d-thermostat]: /iot-hub/Samples/device/PnpDeviceSamples/Thermostat
[d-temperature-controller]: /iot-hub/Samples/device/PnpDeviceSamples/TemperatureController
[thermostat-model]: /iot-hub/Samples/device/PnpDeviceSamples/Thermostat/Models/Thermostat.json
[d-device-info]: https://devicemodels.azure.com/dtmi/azure/devicemanagement/deviceinformation-1.json
[thermostat-hub-qs]: https://docs.microsoft.com/azure/iot-pnp/quickstart-connect-device?pivots=programming-language-csharp
[temp-controller-hub-tutorial]: https://docs.microsoft.com/azure/iot-pnp/tutorial-multiple-components?pivots=programming-language-csharp
[temp-controller-central-tutorial]: https://docs.microsoft.com/azure/iot-central/core/tutorial-connect-device?pivots=programming-language-csharp
