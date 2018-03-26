# Set up IoT Hub

[Azure IoT Hub][iothub-landing] is a fully managed service that enables reliable and secure bi-directional communications between millions of IoT devices and an application back end. Azure IoT Hub offers reliable device-to-cloud and cloud-to-device hyper-scale messaging, enables secure communications using per-device security credentials and access control, and includes device libraries for the most popular languages and platforms.

Before you can communicate with IoT Hub from a device you must create an IoT hub instance in your Azure subscription and then provision your device in your IoT hub. You must complete these steps before you try to run any of the sample IoT Hub device client applications in this repository ([azure-iot-sdks](https://github.com/Azure/azure-iot-sdks)).

## Create an IoT hub

Follow the steps given [here](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted#create-an-iot-hub) to create an IoT Hub.
Your IoT hub is now created, and you have the IoT Hub connection string, which enables applications to perform management operations on the IoT hub such as adding a new device to the IoT hub.

## Provision your device to IoT Hub

You must add details your device to IoT Hub before that device can communicate with the hub. When you add a device to an IoT hub, the hub generates the connection string that the device must use when it establishes the secure connection to the IoT hub.
Follow the steps given [here](./manage_iot_hub.md) to add devices to your IoT Hub.

[iothub-landing]: http://azure.microsoft.com/documentation/services/iot-hub/