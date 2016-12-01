# Samples for the Azure IoT device SDK for .NET

This folder contains simple samples showing how to use the various features of the Microsoft Azure IoT Hub service from a device running .NET code.

## List of samples

   - [Console Sample](ConsoleSample): Shows how to create a console app that connects to IoT Hub and sends messages to a device
   - [UWP Sample](UWPSample): Shows how to create a UWP app that connects to IoT Hub and sends messages to a device

## How to compile and run the samples

Prior to running the samples, you will need to have an [instance of Azure IoT Hub][lnk-setup-iot-hub]  available and a [device Identity created][lnk-manage-iot-hub] in the hub.

It is recommended to leverage the library packages when available to run the samples, but sometimes you will need to compile the SDK for/on your device in order to be able to run the samples.

[This document][devbox-setup] describes in details how to prepare you development environment as well as how to run the samples on Linux, Windows or other platforms.

[devbox-setup]: ../../doc/devbox_setup.md
[lnk-setup-iot-hub]: https://aka.ms/howtocreateazureiothub
[lnk-manage-iot-hub]: https://aka.ms/manageiothub
