# TLS 1.3 Sample

## SCENARIO

Connect to an IoT hub using TLS 1.3 (currently in preview) and then send a telemetry message.

## CONTEXT

### IoT hub support for TLS 1.3
IoT Hub is currently previewing the ability to use TLS 1.3, but accessing that requires some small changes on the client side.

Up until now, connection strings would always follow a format like:

```
<hub name>.azure-devices.<dnsSuffix> 
```

and this IoT hub endpoint supports TLS versions 1.0, 1.1, and 1.2

IoT hub is previewing offering TLS version 1.2 + 1.3 support in endpoints with connection strings like

```
device connection string:
<hub name>.device.azure-devices.<dnsSuffix> 
```

and

```
service connection string:
<hub name>.service.azure-devices.<dnsSuffix> 
```

This sample expects the environment variable "IOTHUB_SERVICE_CONNECTION_STRING" to contain a connection string that follows the service connection string pattern above

### SDK behavior

When you run this sample, the "Client Hello" message in the TLS layer will advertise to IoT hub that this client supports TLS 1.2 and TLS 1.3. In the "Server Hello" response, IoT hub will then choose TLS 1.3 as the version for both client and server to use for this connection.

Note that the SDK behavior here is unchanged as it has always advertised support for TLS 1.2 and TLS 1.3. The only change to make the connection use TLS 1.3 is to connect with the connection string of the host that also supports TLS 1.3. 

## ADDITIONAL CONSIDERATIONS

This feature is not generally available yet nor is it available in preview for all IoT hubs. As such, the above connection string pattern my yield a "host not found" exception.
