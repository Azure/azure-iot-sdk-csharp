# Capturing Traces

## Windows
On Windows logman or PerfView can be used to collect traces. For more information please see https://github.com/dotnet/corefx/blob/master/Documentation/debugging/windows-instructions.md#traces

## Linux
On Linux and OSX LTTNG and perfcollect can be used to collect traces. For more information please see http://blogs.microsoft.co.il/sasha/2017/03/30/tracing-runtime-events-in-net-core-on-linux/

## Console logging
Logging can be added to console. Note that this method will substantially slow down execution.

  1. Add `common\test\ConsoleEventListener.cs` to your project.
  2. Instantiate the listener. Add one or more filters (e.g. `Microsoft-Azure-` or `DotNetty-`):

```C#
	private readonly ConsoleEventListener _listener = new ConsoleEventListener("Microsoft-Azure-");
```
  3. See the `ConsoleEventListener.cs` file to enable colorized logs within Visual Studio Code.

## Azure IoT SDK providers

* `*Microsoft-Azure-Devices-Device-Client {ddbee999-a79e-5050-ea3c-6d1a8a7bafdd}`: DeviceClient related traces.
* `*Microsoft-Azure-Devices-Provisioning-Client {e927240b-7198-5cc8-72f1-7ddcf31bb8cb}`: ProvisioningClient related traces.
* `*Microsoft-Azure-Devices-Provisioning-Transport-Amqp {cc5b923d-ab24-57ee-bec8-d2f5cf1bb6e4}`: ProvisioningTransportHandlerAmqp related traces.
* `*Microsoft-Azure-Devices-Provisioning-Transport-Http {d209b8a1-2e02-5724-f341-677227d0ed22}`: ProvisioningTransportHandlerHttp related traces.
* `*Microsoft-Azure-Devices-Provisioning-Transport-Mqtt {2143dadd-f500-5ff9-12b3-9afacae4d54c}`: ProvisioningTransportHandlerMqtt related traces.
* `*Microsoft-Azure-Devices-Security-Tpm {06e3e7c9-2cd0-57c7-e3b3-c5965ff2736e}`: SecurityProviderTpmHsm related traces.

## Dependency providers

* `*DotNetty-Default {d079e771-0495-4124-bd2f-ab63c2b50525}`: DotNetty related traces (used by the MQTT handlers)
