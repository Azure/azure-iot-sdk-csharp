# Azure IoT SDK Developer Documentation

## 1. Dev machine prerequisites

Before starting development on the Azure IoT SDK for C# you will need to install a few frameworks and tools. Please follow the instructions within [devbox_setup](devbox_setup.md).

## 2. Coding style

Please read and apply our [coding style](coding-style.md) when proposing PRs against the Azure repository.

## 3. Cloning the repository

`git clone https://github.com/Azure/azure-iot-sdk-csharp.git`

## 4. Building the repository

### Building the main repository

* Windows: `build.cmd`
* Linux: `./build.sh`

For more information on running more complex scenarios (like e2e or stress testing) please add the `-h` argument to the build command.

### Building a single project

During development you can use Visual Studio by loading the `azureiot.sln` solution.
If you are using a different editor, you can use the `dotnet` CLI to build (in Windows, Linux or OSX). Here are a few commands. For a complete list see the [.NET Core CLI tools](https://docs.microsoft.com/en-us/dotnet/core/tools/?tabs=netcore2x)

* Build a library csproj: `dotnet build`
* Run tests from a test csproj: `dotnet test`
* Run an application from a csproj (e.g. a sample): `dotnet run`

### ARM32 builds

Our NuGet-packaged SDK can be used certain ARM32-based devices. Since the .NET SDKs are not available on this platform, you will need to cross-compile your application.

Instructions are available here: https://github.com/dotnet/core/blob/master/samples/RaspberryPiInstructions.md

### Xamarin, UWP, Windows IoT

Please follow the [devbox-setup][devbox_setup.md] "Optional Setup" for Xamarin before trying to build. To build and test, run the following Jenkins script from the root of the repository:

`jenkins\windows_csharp_xamarin.cmd`

### .NET Micro Framework

Please follow the [devbox-setup][devbox_setup.md] "Optional Setup" for .NET Micro Framework before trying to build. To build and test, run the following Jenkins script from the root of the repository:

`jenkins\windows_csharp_mf.cmd`

## 5. Testing

### Testing using binaries

Before being able to run our tests, please make sure you read, modify if necessary and run the scripts within the `/test/prerequisites` folders. (e.g. `e2e/test/prerequisites` and `iothub/service/test/prerequisites`).

The CI system will run the `jenkins` scripts from the root folder. (e.g. `jenkins\windows_csharp.cmd`).

Testing requires an Azure subscription and IoT Hub and Provisioning services.

### Testing NuGet packages

The `build` command will always try to produce packages and place them within `bin\pkg`.
Please see `build -h` for instructions on how to set up a local NuGet source and run the e2e tests against the packages instead of the .dll binaries.

## 6. SDK folder structure

### /doc

Contains development instructions for building and changing the Azure IoT SDK for C#.

If you would like to develop an application using the Azure IoT SDK for C# (using pre-built binaries), please follow the dev-guide here: https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-sdks 

### /iothub/device

Contains the IoT Hub Device SDK source, unit-tests and samples. 
This produces the `Microsoft.Azure.Devices.Client` NuGet package.

The src.NetMF folder contains the .NET Microframework port.

### /iothub/service

Contains the IoT Hub Service SDK source, unit-tests and samples.
This produces the `Microsoft.Azure.Devices` NuGet package.

### /provisioning/device

Contains the Provisioning Device SDK source, unit-tests and samples.
This produces the `Microsoft.Azure.Devices.Provisioning.Client` NuGet package.

### /provisioning/device/transport

Contains the Provisioning Device SDK transport libraries source and unit-tests.
`/http` produces the `Microsoft.Azure.Devices.Provisioning.Transport.Http` NuGet package.
`/amqp` produces the `Microsoft.Azure.Devices.Provisioning.Transport.Amqp` NuGet package.
`/mqtt` produces the `Microsoft.Azure.Devices.Provisioning.Transport.Mqtt` NuGet package.

### /provisioning/service

Contains the Provisioning Service SDK source, unit-tests and samples.
This produces the `Microsoft.Azure.Devices.Provisioning.Service` NuGet package.

### /security

Contains the SecurityProvider components for Hardware Security Modules.
`/tpm` produces the `Microsoft.Azure.Devices.Provisioning.Security.Tpm` NuGet package.

### /shared

This contains public API shared across the SDK components.
This produces the `Microsoft.Azure.Devices.Shared` NuGet package.

### /common

Contains common code shared between multiple components. This doesn't generate a separate library and instead may get built-in more than one library.

### /e2e

`/test` contains end-to-end tests run before each PR integration by our internal CI system.
`/stress` contains SDK stress-test applications.

### /jenkins

Contains scripts used by our internal Continuous Integration system (Jenkins).

### /tools

`/DeviceExplorer` contains the source code for the Azure IoT Device Explorer tool. Please see further dev documentation within this folder on how to build and debug.

`/CaptureLogs` contains scripts for capturing SDK traces.
