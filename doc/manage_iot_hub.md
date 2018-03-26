# Manage IoT Hub

Before a device can communicate with IoT Hub, you must add details of that device to the IoT Hub device identity registry. When you add a device to your IoT Hub device identity registry, the hub generates the connection string that the device must use when it establishes its secure connection to your hub. You can also use the device identity registry to disable a device and prevent it from connecting to your hub.

To add devices to your IoT hub and manage those devices, you can use either of:

- The Device Provisioning Service DPS tool
- The Windows-only, graphical Device Explorer tool

Use either of these tools to generate a device-specific connection string that you can copy and paste in the source code of the application running on your device. 
 
> Note: While IoT Hub supports multiple authentication schemes for devices, both these tools generate a pre-shared key to use for authentication.

> Note: You must have an IoT hub running in Azure before you can provision your device. The document [Set up IoT Hub][setup-iothub] describes how to set up an IoT hub.

You can also use both of these tools to monitor the messages that your device sends to an IoT hub and send commands to your devices from IoT Hub.

<a name="dps"/>

## Use the Device Provisioning Service tool to provision a device

Follow the steps [here](https://docs.microsoft.com/en-us/azure/iot-dps/) to learn more about how to provision devices using the Device Provisioning Service.

<a name="device-explorer"/>

## Use the Device Explorer tool to provision a device

For information about using the Device Explorer tool to perform tasks such as disabling a device, monitoring a device, and sending commands to a device see [Using the Device Explorer tool][lnk-device-explorer-docs].


[img-getstarted1]: media/device_explorer/iotgetstart1.png
[img-getstarted2]: media/device_explorer/iotgetstart2.png
[img-getstarted3]: media/device_explorer/iotgetstart3.png
[img-getstarted4]: media/device_explorer/iotgetstart4.png
[img-connstr]: media/device_explorer/connstr.png

[lnk-this-repo]: https://github.com/Azure/azure-iot-sdks
[setup-iothub]: setup_iothub.md
[lnk-install-iothub-explorer]: https://github.com/Azure/iothub-explorer#installing-iothub-explorer
[lnk-iothub-explorer-identity]: https://github.com/Azure/iothub-explorer#working-with-the-device-identity-registry
[lnk-iothub-explorer-devices]: https://github.com/Azure/iothub-explorer#working-with-devices
[lnk-releasepage]: https://github.com/Azure/azure-iot-sdks-preview/releases
[lnk-device-explorer-docs]: ../tools/DeviceExplorer/readme.md
