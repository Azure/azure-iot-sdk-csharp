# Microsoft Azure IoT device SDK for .NET

This folder contains the following
* The Microsoft Azure IoT device SDK for .NET to facilitate building devices and applications that connect to and are managed by Azure IoT Hub services.
* Documentation and samples to help you get started using this SDK.

The library is available as a NuGet package for you include in your own development projects.

## Features
 * Sends event data to Azure IoT based services.
 * Maps server commands to device functions.
 * Batches messages to improve communication efficiency.
 * Supports pluggable transport protocols.

## Application development guidelines
For information on how to use this library refer to the documents below:
- [How to use the Azure IoT SDKs for .NET][how-to-use]

### Using client SDK in [Azure Functions][azure-functions]
Azure Function doesn't currently support [bindingRedirect][binding-redirect] **element**

**Example**

```
<dependentAssembly>
        <assemblyIdentity name="Validation" publicKeyToken="2fc06f0d701809a7" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-2.2.0.0" newVersion="2.2.0.0" />
</dependentAssembly>
```

So **workaround** would be to directly install older version of **validation.dll** using [project.json][project-json] covered under [Package management][package-management] 

**Example**

```
{
"frameworks": {
    "net46":{
        "dependencies": {
            "Microsoft.Azure.Devices.Client": "1.1.1",
             "Validation": "2.0.6.15003"
                    }
                }
        }
}
```
> Note: This is covered in more detail under GitHub issue [#978] [github-issue-978]

Other useful documents include:
- [Setup IoT Hub][setup-iothub]
- [Microsoft Azure IoT device SDK for .NET API reference][dotnet-api-ref]


## API reference

API reference documentation can be found online at https://msdn.microsoft.com/library/microsoft.azure.devices.aspx.

[setup-iothub]: ../../doc/setup_iothub.md
[devbox-setup]: ../../doc/devbox_setup.md
[run-sample-on-desktop-windows]: ../../doc/get_started/windows-desktop-csharp.md
[run-sample-on-windows-iot-core]: ../../doc/get_started/windows10-iotcore-csharp.md
[dotnet-api-ref]: https://msdn.microsoft.com/library/microsoft.azure.devices.aspx
[azure-functions]: https://azure.microsoft.com/services/functions/
[binding-redirect]:https://msdn.microsoft.com/library/eftw1fys(v=vs.110).aspx
[github-issue-978]: https://github.com/Azure/azure-iot-sdks/issues/978
[project-json]:https://docs.microsoft.com/azure/azure-functions/functions-reference-csharp
[package-management]: https://docs.microsoft.com/azure/azure-functions/functions-reference-csharp#package-management
[how-to-use]: ../../readme.md#how-to-use-the-azure-iot-sdks-for-net
