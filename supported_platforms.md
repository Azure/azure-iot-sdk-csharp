# Microsoft Azure IoT SDKs for .NET

This SDK is tested nightly on a mix of .NET implementations on both Windows 10 and on Ubuntu 20.04. For additional details for each tested platform, see the respective sections below. 

## Supported .NET versions

The NuGet packages provide support for the following .NET versions:
- .NET 5.0
- .NET Standard 2.1
- .NET Standard 2.0
- .NET Framework 4.7.2 (IoT Hub SDKs only)
- .NET Framework 4.5.1 (IoT Hub SDKs only)

This SDK _may_ work with newer versions of .NET, but there are no guarantees that they will _always_ work for those until we officially add support for them nor are there guarantees that we will fix bugs that are only present on those versions.

Note that applications will resolve the dll that is being referenced based on the framework precedence rules. This means that .NET Framework targeting applications will look for the closet .NET Framework dll. In the absence of that, it will pick up the closest .NET Standard dll. Similarly for netcoreapp applications will look for the closest netcoreapp dll and in the absence of one will pick the closest .NET Standard dll. Since we publish the above list of dlls, you should target the appropriate net target to ensure you get the desired .NET API coverage.

## Windows 10

Note that, while we only directly test on Windows 10, we do support other Windows versions officially supported by Microsoft.

Nightly test platform details:

.NET versions tested on
- .NET 5.0
- .NET Core 3.1
- .NET Core 2.1.30
- .NET Framework 4.7.2 (only IoT Hub SDKs tested)
- .NET Framework 4.5.1 (only IoT Hub SDKs tested)


Default locale: en_US, platform encoding: Cp1252

OS name: "windows server 2022", version: "10.0", arch: "amd64", family: "windows"

## Ubuntu 20.04

Note that, while we only directly test on Ubuntu 20.04, we do generally support other [Linux distributions supported by .NET core](https://docs.microsoft.com/dotnet/core/install/linux). 

Nightly test platform details:

.NET versions tested on:
- .NET 5.0
- .NET Core 3.1
- .NET Core 2.1.30

Default locale: en_US, platform encoding: UTF-8

OS name: "linux", version: "5.8.0-1040-azure", arch: "amd64", family: "unix"

## Miscellaneous support notes

- This library does not officially support being run on MacOS.
- This library does not officially support being run in Xamarin applications.
- .NET Standard 1.3 (IoT Hub SDKs only) is last supported in the [2020-02-27](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/2020-2-27) and in the [2020-1-31 LTS](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-1-31) releases.
- [.NET Core Runtime ID Catalog](https://docs.microsoft.com/dotnet/core/rid-catalog)
- In order to run this SDK your device will need to meet the [.NET Framework System Requirements](https://docs.microsoft.com/dotnet/framework/get-started/system-requirements)
