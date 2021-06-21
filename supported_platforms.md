# Microsoft Azure IoT SDKs for .NET

This SDK is tested nightly on a mix of .NET frameworks on both Windows 10 and on Ubuntu 1604. For additional details
for each tested platform, see the respective sections below. 

## Supported .NET versions

The NuGet packages provide support for the following .NET versions:
- .NET Standard 2.1
- .NET Standard 2.0
- .NET Framework 4.7.2 (IoT Hub SDKs only)
- .NET Framework 4.5.1 (IoT Hub SDKs only)

This SDK _may_ work with other versions of .NET, but there are no guarantees that they will _always_ work for those 
versions nor are there guarantees that we will fix bugs that are only present on those versions.

## Windows 10

Note that, while we only directly test on Windows 10, we do support other Windows versions officially supported by Microsoft.

Nightly test platform details:

.NET versions tested on
- .NET Core 3.1
- .NET Core 2.1.18
- .NET Framework 4.7.2 (only IoT Hub SDKs tested)
- .NET Framework 4.5.1 (only IoT Hub SDKs tested)


Default locale: en_US, platform encoding: Cp1252

OS name: "windows server 2016", version: "10.0", arch: "amd64", family: "windows"

## Ubuntu 1604

Note that, while we only directly test on Ubuntu 1604, we do generally support other [Linux distributions supported by .NET core](https://docs.microsoft.com/en-us/dotnet/core/install/linux). 

Nightly test platform details:

.NET versions tested on:
- .NET Core 3.1
- .NET Core 2.1.18

Default locale: en_US, platform encoding: UTF-8

OS name: "linux", version: "4.15.0-1113-azure", arch: "amd64", family: "unix"

## Miscellaneous support notes

- This library has a [preview version](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/preview_2021-6-8) that supports .NET 5.0, but we don't officially support it in our main releases yet.
- This library does not officially support being run on MacOS.
- This library does not officially support being run in Xamarin applications.
- .NET Standard 1.3 (IoT Hub SDKs only) is last supported in the [2020-02-27](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/2020-2-27) and in the [2020-1-31 LTS](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/lts_2020-1-31) releases.