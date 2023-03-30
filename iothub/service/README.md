# Microsoft Azure IoT service SDK for C\# #

This folder contains the following 
* The Azure IoT service SDK for .NET to easily and securely manage an instance of the Microsoft Azure IoT Hub service as well as send Cloud to Device messages through IOT Hub.
* Samples showing how to use the SDK

## Features

* Create/remove/update/list device identities in your IoT hub
* Send messages to your devices and get feedback when they're delivered
<!-- 
    * Implements CRUD operations on Azure IoT Hub device registry
    * Interact with a Device Twins from a back-end application
    * Invoke a Cloud to Device direct Method 
    * Implements sending a Cloud to Device message -->

## Usage

The library is available as a NuGet package: **Microsoft.Azure.Devices**
For a full step by step guide on how to use the library, checkout this [article](https://azure.microsoft.com/documentation/articles/iot-hub-csharp-csharp-getstarted/)

## References

To learn more on developing for the Azure IoT Hub service, visit our [developer guide](https://azure.microsoft.com/documentation/articles/iot-hub-devguide/)

[This document][devbox-setup] describes in details how to prepare you development environment as well as how to run the samples on Linux, Windows or other platforms.

## Samples

The repository contains a set of simple samples that will help you get started.
You can find a list of these samples [here][samples]. 

[devbox-setup]: ../../doc/devbox_setup.md
[samples]: ./samples/
