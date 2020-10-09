# Plug And Play Device Samples

These samples demonstrate how a [plug and play convention][pnp-convention] enabled device interacts with IoT hub, to:

- Send telemetry.
- Update read-only and read-write porperties.
- Respond to command invocation.

The samples demonstrate two scenarios:

- The device is modeled as a plug and play device, having only a root interface - [Thermostat][d-thermostat]
  - This model defines root level telemetry, read-only and read-write properties and commands.
- The device is modeled as a plug and play device having multiple components - [Temperature controller][d-temperature-controller].
  - This model defines root level telemetry, read-only property and commands.
  - It also defines two [Thermostat][thermostat-model] components, and a [device information][d-device-info] component.

## Configuring the samples in Visual Studio

These samples use the `launchSettings.json` in VS to configure different configuration settings, one for direct connection strings and one for DPS.
The file is committed as `launchSettings.template.json`, you must rename it to `launchSettings.json` and then you can configure it from the Debug tab in the project properties.

## Configuring the samples in VSCode

These samples use the `launch.json` in VSCode to configure different configuration settings, one for direct connection strings and one for DPS.
The file is committed as `launch.template.json`, you must rename it to `launch.json` to make effect when starting a debugging session

[pnp-convention]: https://docs.microsoft.com/azure/iot-pnp/concepts-convention
[d-thermostat]: /iot-hub/Samples/device/PnpDeviceSamples/Thermostat
[d-temperature-controller]: /iot-hub/Samples/device/PnpDeviceSamples/TemperatureController
[thermostat-model]: /iot-hub/Samples/device/PnpDeviceSamples/Thermostat/Models/Thermostat.json
[d-device-info]: https://devicemodels.azureiotsolutions.com/models/public/dtmi:azure:DeviceManagement:DeviceInformation;1?codeView=true