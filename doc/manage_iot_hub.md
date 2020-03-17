# Manage IoT Hub

Before a device can communicate with IoT Hub, you must add details of that device to the IoT Hub device identity registry.
When you add a device to your IoT Hub device identity registry, the hub generates the connection string that the device must use when it establishes its secure connection to your hub.
You can also use the device identity registry to disable a device and prevent it from connecting to your hub.

To add devices to your IoT hub and manage those devices, you can use the Azure Portal experience, the Device Provisioning Service DPS tool, or the Azure IoT Explorer tool.

Use any of these tools to generate a device-specific connection string that you can copy and paste in the source code of the application running on your device.

> Note: While IoT Hub supports multiple authentication schemes for devices, both these tools generate a pre-shared key to use for authentication.

> Note: You must have an IoT hub running in Azure before you can provision your device. The document [Set up IoT Hub](./setup_iothub.md) describes how to set up an IoT hub.

## Use the Azure Portal to provision a device

Follow the steps [here](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal#register-a-new-device-in-the-iot-hub) to learn how to provision devices using the Azure Portal experience.

## Use the Device Provisioning Service tool to provision a device

Follow the steps [here](https://docs.microsoft.com/en-us/azure/iot-dps/) to learn more about how to provision devices using the Device Provisioning Service.

## Use Azure IoT Explorer to provision a device

Learn more about the tool [here](https://github.com/Azure/azure-iot-explorer).

> Note: you can also use this to monitor the messages that your device sends to an IoT hub, and send commands to the device.