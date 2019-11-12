# Azure IoT Digital Twins Service SDK

**PREVIEW - WILL LIKELY HAVE BREAKING CHANGES**

## Description

For a basic outline of what Azure IoT Plug and Play is, see [this documentation](https://docs.microsoft.com/en-us/azure/iot-pnp/overview-iot-plug-and-play)

## API reference

To be written soon

## Installation

Install the Azure Digital Twin Service Client library for .NET with NuGet:

```
dotnet add package Azure.IoT.DigitalTwin.Service
```

## Samples

For samples on how to do each of these, check [this folder](./sample)

## Notes 

The Digital Twin Service Client is built on an auto-generated protocol layer, and this readme contains the config used by autorest to generate that protocol layer

The powershell script `generateCode.ps1` is used to generate this protocol layer using autorest and a few post-autorest modifications. This script uses the swagger definition in the `serviceDigitalTwinOnly.json` file when generating the protocol layer.

> see https://aka.ms/autorest

``` yaml 
input-file: serviceDigitalTwinOnly.json

csharp:
  namespace: Azure.IoT.DigitalTwin.Service.Generated
  output-folder: Generated
  add-credentials: true                
  use-internal-constructors: true
  sync-methods: none
```