# Azure IoT SDK Developer Documentation

## 1. Dev machine prerequisites

Before starting development on the Azure IoT SDK for .NET you will need to install a few frameworks and tools. Please follow the instructions within [devbox_setup](devbox_setup.md).

## 2. Design Guidelines and Coding Style

### Design
We are following the [Azure SDK design specification for .NET](https://azuresdkspecs.z5.web.core.windows.net/DotNetSpec.html). To preserve backward compatibility, existing code will not change to follow these rules.

#### Using [`IDisposable`](https://docs.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-5.0#implementing-idisposable) types:
- If the sdk implements `class A` that owns an `IDisposable` resource `X`, then `A` should also be `IDisposable`. In that case, resource `X` can be safely disposed when `class A` is disposed.
- The sdk should dispose any `IDisposable` resource that it creates.
- The sdk should not dispose an `IDisposable` resource that is supplied by the calling application. This is because the caller might want to reuse the resource elsewhere in the application. The responsibility of disposal of caller-supplied `IDisposable` resource is on the caller. An example of such a resource would be an `X509Certificate` instance that is used for authenticating our clients.

### Code style
Please read and apply our [coding style](coding-style.md) when proposing PRs against the Azure repository. When changing existing files, please apply changes to the entire file. Otherwise, maintain the same style.

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

Please follow the [devbox-setup](devbox_setup.md) "Optional Setup" for Xamarin before trying to build. To build and test, run the following Jenkins script from the root of the repository:

`jenkins\windows_csharp_xamarin.cmd`

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

Contains development instructions for building and changing the Azure IoT SDK for .NET.

If you would like to develop an application using the Azure IoT SDK for .NET (using pre-built binaries), please follow the dev-guide here: https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-sdks 

### Package architecture
![packages](https://www.plantuml.com/plantuml/png/0/jLLRRi8m4Fn7oXtyNv4J80G2LLMbI2tG0xZs9bWuSLYlaTAAkpSXK48W9PJWtvsTcNtO7bdYI2xMNi_hQGY9aM6eeYKngH04APCKePIB5O-01KgWiIOaV_pb4FmfR9G0wy-N746omU3PQ0au7B9lhxTjaLbBOVaHcblBve05OA8L97G85UU9JRHniZ1QSfIXdTWnVGQHieHPm9DS7Mi4iuyf2mqoMPgeniQEJCn9YPAyp8zp3nTbNisdFRMuRLUt_uPcespUNfL4_hxOncPKmMUD-TKzujyTO7OIQ-LfpzdaeeITbLk514Ow3Hrqv8gLAhR1rksQ2-I9JGtce7YT_cEPc-Y2DL67T2z4zxkRWt2eAFCNQLmZszCrtTX-VtZb7MZCFOpr7efBQz8Pt-4YTaPOczfVl1SAkrbajxYF5jcjysD4JhQopH16aCZy-_e1 "packages")

### /iothub/device

Contains the IoT Hub Device SDK source, unit-tests and samples. 
This produces the `Microsoft.Azure.Devices.Client` NuGet package.

```diff
- .NET MicroFramework is no longer supported in the SDK.
```

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

### /shared

This contains public API shared across the SDK components.
This produces the `Microsoft.Azure.Devices.Shared` NuGet package.

### /common

Contains common code shared between multiple components. This doesn't generate a separate library and instead may get built-in more than one library.

### /e2e

`/test` contains end-to-end tests run before each PR integration by our internal CI system.
`/stress` contains SDK stress-test applications.

### /vsts

Contains scripts used by our internal Continuous Integration system (Azure DevOps).

### /tools

`/CaptureLogs` contains scripts for capturing SDK traces.
