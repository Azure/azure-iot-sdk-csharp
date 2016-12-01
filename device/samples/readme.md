# Samples for the Azure IoT device SDK for .NET

This folder contains simple samples showing how to use the various features of the Microsoft Azure IoT Hub service from a device running .NET code.

## List of samples

   - [Simple .NET sample using AMQP](DeviceClientAmqpSample): Shows how to connect to IoT Hub and send and receive raw messages using the AMQP protocol.
   - [Simple .NET sample using HTTP](DeviceClientHttpSample): Shows how to connect to IoT Hub and send and receive raw messages using the HTTP protocol.
   - [Simple .NET sample using MQTT](DeviceClientMqttSample): Shows how to connect to IoT Hub and send and receive raw messages using the MQTT protocol.
   - [Simple .NET sample for iOS](DeviceClientSampleiOS): Shows how to connect to IoT Hub and send and receive raw messages from a device that runs iOS.
   - [Simple .NET sample for Android](DeviceClientSampleAndroid): Shows how to connect to IoT Hub and send and receive raw messages from a device that runs Android.
   - [Simple UWP C++ Sample for File Upload](CppUWPSample): Shows how to connect to IoT Hub and upload a file.
   - [Simple UWP C++ sample](CppUWPSample): Shows how to connect to IoT Hub and send and receive raw messages in a C++ [UWP](https://msdn.microsoft.com/windows/uwp/get-started/whats-a-uwp) (Universal Windows Platform) application.
   - [Simple UWP JS sample](JSSample): Shows how to connect to IoT Hub and send and receive raw messages in a JavaScript [UWP](https://msdn.microsoft.com/windows/uwp/get-started/whats-a-uwp) application.
   - [Simple UWP C# sample](UWPSample): Shows how to connect to IoT Hub and send and receive raw messages in a C# [UWP](https://msdn.microsoft.com/windows/uwp/get-started/whats-a-uwp) application.
   - [Simple .NET Micro Framework 4.3 sample](NetMFDeviceClientHttpSample_43): Shows how to connect to IoT Hub and send and receive raw messages from a device running .NET Micro Framework 4.3.
   - [Simple .NET Micro Framework 4.2 sample](NetMFDeviceClientHttpSample_42): Shows how to connect to IoT Hub and send and receive raw messages from a device running .NET Micro Framework 4.2.

## How to compile and run the samples

Prior to running the samples, you will need to have an [instance of Azure IoT Hub][lnk-setup-iot-hub]  available and a [device Identity created][lnk-manage-iot-hub] in the hub.

It is recommended to leverage the library packages when available to run the samples, but sometimes you will need to compile the SDK for/on your device in order to be able to run the samples.

The following documents provide more information about how to compile and run the samples:
 - [Prepare you development environment as well as how to run the samples on Linux, Windows or other platforms.][devbox-setup]
 - [Run the Device Client AMQP sample on your Windows 10 desktop][run-sample-on-desktop-windows]
 - [Run the Device Client AMQP sample on  Windows 10 IOT][run-sample-on-windows-iot-core]
 - [Create a Xamarin sample for Android and iOS][create-csharp-pcl]

[devbox-setup]: ../doc/devbox_setup.md
[run-sample-on-desktop-windows]: ../doc/windows-desktop-csharp.md
[run-sample-on-windows-iot-core]: ../doc/windows10-iotcore-csharp.md
[create-csharp-pcl]: ../doc/csharp-pcl.md
[lnk-setup-iot-hub]: https://aka.ms/howtocreateazureiothub
[lnk-manage-iot-hub]: https://aka.ms/manageiothub