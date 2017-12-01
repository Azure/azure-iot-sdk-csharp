# Capturing Traces

## Windows
On Windows logman or PerfView can be used to collect traces. For more information please see https://github.com/dotnet/corefx/blob/master/Documentation/debugging/windows-instructions.md#traces 

## Linux
On Linux and OSX LTTNG and perfcollect can be used to collect traces. For more information please see http://blogs.microsoft.co.il/sasha/2017/03/30/tracing-runtime-events-in-net-core-on-linux/

## Azure IoT SDK providers

* `*Microsoft-Azure-Devices-Provisioning-Client {e927240b-7198-5cc8-72f1-7ddcf31bb8cb}`: ProvisioningClient related traces.
* `*Microsoft-Azure-Devices-Provisioning-Transport-Amqp {cc5b923d-ab24-57ee-bec8-d2f5cf1bb6e4}`: ProvisioningTransportHandlerAmqp related traces.
* `*Microsoft-Azure-Devices-Provisioning-Transport-Http {d209b8a1-2e02-5724-f341-677227d0ed22}`: ProvisioningTransportHandlerHttp related traces.
* `*Microsoft-Azure-Devices-Provisioning-Transport-Mqtt {2143dadd-f500-5ff9-12b3-9afacae4d54c}`: ProvisioningTransportHandlerMqtt related traces.

## Dependency providers

* `*DotNetty-Default {d079e771-0495-4124-bd2f-ab63c2b50525}`: DotNetty related traces (used by the MQTT handlers)
