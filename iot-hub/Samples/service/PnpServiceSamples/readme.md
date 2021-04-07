---
page_type: sample
description: "A set of samples that show how to manage a device that uses the IoT Plug and Play conventions."
languages:
- csharp
products:
- azure
- azure-iot-hub
- azure-iot-pnp
- dotnet
urlFragment: azure-iot-pnp-service-samples-for-csharp-net
---

# IoT Plug And Play service samples

These samples demonstrate how to manage a device that follows the [IoT Plug and Play conventions][pnp-convention] from IoT Hub. The samples show how to:

- Read read-only properties.
- Write read-write properties.
- Invoke commands.

The samples demonstrate two scenarios:

- Manage an IoT Plug and Play device that implements the [Thermostat][d-thermostat] model. This model has a single interface that defines telemetry, read-only and read-write properties, and commands.
- Manage an IoT Plug and Play device that implements the [Temperature controller][d-temperature-controller] model. This model uses multiple components:
  - The top-level interface defines telemetry, read-only property and commands.
  - The model includes two [Thermostat][thermostat-model] components, and a [device information][d-device-info] component.

## Configuring the samples in Visual Studio

These samples use the `launchSettings.json` in Visual Studio for configuration settings.

The configuration file is committed to the repository as `launchSettings.template.json`. Rename the file to `launchSettings.json` and then configure it from the **Debug** tab in the project properties.

## Configuring the samples in VSCode

These samples use the `launch.json` in Visual Studio Code for configuration settings.

The configuration file is committed to the repository as `launch.template.json`. Rename it to `launch.json` to take effect when you start a debugging session.

## Quickstarts and tutorials

To learn more about how to configure and run the Thermostat service sample with IoT Hub, see [Quickstart: Interact with an IoT Plug and Play device that's connected to your solution][thermostat-hub-qs].

[pnp-convention]: https://docs.microsoft.com/azure/iot-pnp/concepts-convention
[d-thermostat]: /iot-hub/Samples/device/PnpDeviceSamples/Thermostat
[d-temperature-controller]: /iot-hub/Samples/device/PnpDeviceSamples/TemperatureController
[thermostat-model]: /iot-hub/Samples/device/PnpDeviceSamples/Thermostat/Models/Thermostat.json
[d-device-info]: https://devicemodels.azure.com/dtmi/azure/devicemanagement/deviceinformation-1.json
[thermostat-hub-qs]: https://docs.microsoft.com/azure/iot-pnp/quickstart-service?pivots=programming-language-csharp
