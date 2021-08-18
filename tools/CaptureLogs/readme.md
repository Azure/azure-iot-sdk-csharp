# Capturing Traces

## Windows
On Windows logman or PerfView can be used to collect traces. For more information please see https://github.com/dotnet/runtime/blob/master/docs/workflow/debugging/libraries/windows-instructions.md#traces

### Send Traces to Application Insights

We have created a tool to send diagnostic traces logs to [Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview) from the target machine without the need to add Application Insights to your target application. This tool takes advantage of the real time session and does not require disk space.

1. Download the TransmitETL.zip from this directory.
2. Unzip the file into a location you can access
3. Create a `logman` trace session for the IoT SDK trace providers using our providers file.
4. Start the `logman` trace
5. Run `TransmitETL.exe` from the directory you created in step 2
   1. Supply `--sessionname` with the session name created above
   2. Supply `--connectionstring` with a [connection string to Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/sdk-connection-string?tabs=net)

```
logman create trace IotTrace -rt -pf .\iot_providers.txt
```

```
TransmitETL --sessionname IotTrace --connectionstring <<APPLICATIONINSIGHTS CONNECTION STRING>>
```

## Linux
On Linux and OSX LTTNG and perfcollect can be used to collect traces. For more information please see https://github.com/dotnet/runtime/blob/master/docs/project/linux-performance-tracing.md

## Console logging
Logging can be added to console. Note that this method will substantially slow down execution.

  1. Add [`e2e\test\helpers\ConsoleEventListener.cs`](https://github.com/Azure/azure-iot-sdk-csharp/blob/master/e2e/test/helpers/ConsoleEventListener.cs) to your project.
  2. Instantiate the listener. Add one or more filters (e.g. `Microsoft-Azure-` or `DotNetty-`):

```csharp
	private static readonly ConsoleEventListener _listener = new ConsoleEventListener();
```
> NOTE: 
> 1. `static` fields are optimized for runtime performance and are initialized prior to their first usage. If `_listener` is the only static field initialized in your class, you'll need to provide a static constructor that initializes them when the class is loaded.
> 2. `ConsoleEventListener.cs` logs the following events by default. If you want to log specific event providers, modify the [event filter](https://github.com/Azure/azure-iot-sdk-csharp/blob/4b5e0147f3768761cacaf4913ab6be707425f9da/e2e/test/helpers/ConsoleEventListener.cs#L20) list to include only your desired event providers.
> ```csharp
> private static readonly string[] s_eventFilter = new string[] { "DotNetty-Default", "Microsoft-Azure-Devices", "Azure-Core", "Azure-Identity" };
> ```

## Azure IoT SDK providers

* `*Microsoft-Azure-Devices-Device-Client {ddbee999-a79e-5050-ea3c-6d1a8a7bafdd}`: DeviceClient related traces.
* `*Microsoft-Azure-Devices-Service-Client {1a3d8d74-0a87-550c-89d7-b5d40ccb459b}`: ServiceClient related traces.
* `*Microsoft-Azure-Devices-Provisioning-Client {e927240b-7198-5cc8-72f1-7ddcf31bb8cb}`: ProvisioningClient related traces.
* `*Microsoft-Azure-Devices-Provisioning-Transport-Amqp {cc5b923d-ab24-57ee-bec8-d2f5cf1bb6e4}`: ProvisioningTransportHandlerAmqp related traces.
* `*Microsoft-Azure-Devices-Provisioning-Transport-Http {d209b8a1-2e02-5724-f341-677227d0ed22}`: ProvisioningTransportHandlerHttp related traces.
* `*Microsoft-Azure-Devices-Provisioning-Transport-Mqtt {2143dadd-f500-5ff9-12b3-9afacae4d54c}`: ProvisioningTransportHandlerMqtt related traces.
* `*Microsoft-Azure-Devices-Security-Tpm {06e3e7c9-2cd0-57c7-e3b3-c5965ff2736e}`: SecurityProviderTpmHsm related traces.

## Azure IoT SDK test providers

* `*Microsoft-Azure-Devices-TestLogging {f7ac322b-77f1-5a2d-0b56-ec79a41e82a2}`: E2E Test logging related traces

## Dependency providers

* `*DotNetty-Default {d079e771-0495-4124-bd2f-ab63c2b50525}`: DotNetty related traces (used by the MQTT handlers)
