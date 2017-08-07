 Samples for the Azure IoT device SDK for .NET

This folder contains simple samples showing how to use the various features of the Microsoft Azure IoT Hub service from a device running .NET.

## List of samples

* Simple send and receive messages:
   * **DeviceClientMqttSample**: send and receive messages from a single device over an MQTT connection
   * **DeviceClientAmqpSample**: send and receive messages from a single device over an AMQP connection
   * **DeviceClientHttpSample**: send and receive messages from a single device over an HTTP connection
   * Websocket examples?

* Multiplexing send and receive of several devices over a single connection (useful in Gateway scenarios where multiplexing might be needed):
   * Samples?

* Device services samples (Device Twins, Methods, and Device Management):
   * **DeviceClientTwinSample**: Enable Twin and Direct Methods on AMQP
   * **DeviceClientMethodSample**: Enable Twin and Direct Methods on AMQP

* Uploading blob to Azure:
   * Samples?

Samples:
   * **DeviceClientFileUploadSample**:
   * **DeviceClientKeysRolloverSample**:
   * **DeviceClientSampleAndroid**:
   * **DeviceClientSampleiOS**:
   * **NetMFDeviceCLientHttpSample_43**:
   * **NetMFDeviceClientHttpSample_44**:
   * **UWPSample**:


## How to compile and run the samples

Prior to running the samples, you will need to have an [instance of Azure IoT Hub][lnk-setup-iot-hub]  available and a [device Identity created][lnk-manage-iot-hub] in the hub.

It is recommended to leverage the library packages when available to run the samples, but sometimes you will need to compile the SDK for/on your device in order to be able to run the samples.

[This document][devbox-setup] describes in detail how to prepare your development environment as well as how to run the samples on Linux, Windows or other platforms.


[devbox-setup]: ../doc/devbox_setup.md
[lnk-setup-iot-hub]: https://aka.ms/howtocreateazureiothub
[lnk-manage-iot-hub]: https://aka.ms/manageiothub