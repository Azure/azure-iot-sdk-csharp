### Digital Twin Device Samples

These samples demonstrate how a [plug and play convention](pnp-convention) enabled device interacts with IoT hub, to:
- Send telemetry.
- Update read-only and read-write porperties.
- Respond to command invocation. 

The samples demonstrate two scenarios:
- The device is modeled as a plug and play device, having only a root interface - [Thermostat][d-thermostat]
  - This model defines root level telemetry, read-only and read-write properties and commands.
- The device is modeled as a plug and play device having multiple components - [Temperature controller][d-temperature-controller].
  - This model defines root level telemetry, read-only property and commands.
  - It also defines two [Thermostat][thermostat-model] components, and a [device information][d-device-info] component.

[pnp-convention]: https://docs.microsoft.com/en-us/azure/iot-pnp/concepts-convention
[d-thermostat]: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/device/samples/DigitalTwinDeviceSamples/Thermostat
[d-temperature-controller]: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/device/samples/DigitalTwinDeviceSamples/TemperatureController
[thermostat-model]: /iothub/device/samples/DigitalTwinDeviceSamples/Thermostat/Models/Thermostat.json
[d-device-info]: https://devicemodels.azureiotsolutions.com/models/public/dtmi:azure:DeviceManagement:DeviceInformation;1?codeView=true