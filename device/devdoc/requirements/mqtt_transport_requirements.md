# Mqtt Transport Requirements

## Overview

The Mqtt Transport is used to communicate between the DeviceClient object and the network 

## References


##Exposed API
```csharp

sealed class MqttTransportHandler : TransportHandler
{
    public static MqttTransportHandler Create(string hostname, IAuthenticationMethod authMethod);
    public static MqttTransportHandler CreateFromConnectionString(string connectionString);
    public override async Task OpenAsync(bool explicitOpen, CancellationToken cancellationToken);
    public override async Task SendEventAsync(Message message, CancellationToken cancellationToken);
    public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken);
    public override async Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken);
    public override async Task CompleteAsync(string lockToken, CancellationToken cancellationToken);
    public override Task AbandonAsync(string lockToken, CancellationToken cancellationToken);
    public override Task RejectAsync(string lockToken, CancellationToken cancellationToken);
    public override async Task CloseAsync();

    public override Task EnableMethodsAsync(CancellationToken cancellationToken);
    public override Task SendMethodResponseAsync(Method method, CancellationToken ct);

    public override Task EnableTwinAsync(CancellationToken cancellationToken);
    public override Task SendTwinGetAsync(Twin twin, CancellationToken ct);
    public override Task SendTwinUpdateAsync(Twin twin, CancellationToken ct);
}

```

### EnableMethodsAsync
```csharp
public override Task EnableMethodsAsync(CancellationToken cancellationToken);
```

**SRS_CSHARP_MQTT_TRANSPORT_18_001: [** `EnableMethodsAsync` shall subscribe using the '$iothub/methods/POST/' topic filter. **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_002: [** `EnableMethodsAsync` shall wait for a response to the subscription request. **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_003: [** `EnableMethodsAsync` shall return failure if the subscription request fails. **]**



### SendMethodResponseAsync
```csharp
public override Task SendMethodResponseAsync(Method method, CancellationToken ct);
```

**SRS_CSHARP_MQTT_TRANSPORT_18_005: [** `SendMethodResponseAsync` shall allocate a `Message` object containing the method response. **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_006: [** `SendMethodResponseAsync` shall set the message topic to '$iothub/methods/res/<STATUS>/?$rid=<REQUEST_ID>' where STATUS is the return status for the method and REQUEST_ID is the request ID received from the service in the original method call. **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_007: [** `SendMethodResponseAsync` shall set the message body to the response payload of the `Method` object. **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_008: [** `SendMethodResponseAsync` shall send the message to the service. **]**


### EnableTwinAsync
```csharp
public override Task EnableTwinAsync(CancellationToken cancellationToken);
```

**SRS_CSHARP_MQTT_TRANSPORT_18_009: [** `EnableTwinAsync` shall subscribe using the '$iothub/twin/res/#' topic filter. **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_010: [** `EnableTwinAsync` shall subscribe using the '$iothub/twin/PATCH/properties/desired/#' topic filter. **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_011: [** `EnableTwinAsync` shall wait for responses on both subscriptions. **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_012: [** If either subscription request fails, `EnableTwinAsync` shall return failure **]**


### SendTwinGetAsync
```csharp
public override Task SendTwinGetAsync(Twin twin, CancellationToken ct);
```

**SRS_CSHARP_MQTT_TRANSPORT_18_014: [** `SendTwinGetAsync` shall allocate a `Message` object to hold the `GET` request **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_015: [** `SendTwinGetAsync` shall generate a GUID to use as the $rid property on the request **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_016: [** `SendTwinGetAsync` shall set the `Message` topic to '$iothub/twin/GET/?$rid=<REQUEST_ID>' where REQUEST_ID is the GUID that was generated **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_017: [** `SendTwinGetAsync` shall wait for a response from the service with a matching $rid value **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_018: [** When a response is received, `SendTwinGetAsync` shall send it to the caller using the `TwinUpdateHandler`. **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_019: [** If the response is failed, `SendTwinGetAsync` shall return that failure to the caller. **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_020: [** If the response doesn't arrive within `MqttTransportHandler.TwinTimeout`, `SendTwinGetAsync` shall fail with a timeout error **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_021: [** If the response contains a success code, `SendTwinGetAsync` shall return success to the caller **]** 


### SendTwinUpdateAsync
```csharp
public override Task SendTwinUpdateAsync(Twin twin, TwinProperties properties, CancellationToken ct);
```

**SRS_CSHARP_MQTT_TRANSPORT_18_022: [** `SendTwinUpdateAsync` shall allocate a `Message` object to hold the update request **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_023: [** `SendTwinUpdateAsync` shall generate a GUID to use as the $rid property on the request **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_024: [** `SendTwinUpdateAsync` shall set the `Message` topic to '$iothub/twin/PATCH/properties/reported/?$rid=<REQUEST_ID>' where REQUEST_ID is the GUID that was generated **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_025: [** `SendTwinUpdateAsync` shall serialize the `properties` object into a JSON string **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_026: [** `SendTwinUpdateAsync` shall set the body of the message to the JSON string **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_027: [** `SendTwinUpdateAsync` shall wait for a response from the service with a matching $rid value **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_028: [** If the response is failed, `SendTwinUpdateAsync` shall return that failure to the caller. **]**

**SRS_CSHARP_MQTT_TRANSPORT_18_029: [** If the response doesn't arrive within `MqttTransportHandler.TwinTimeout`, `SendTwinUpdateAsync` shall fail with a timeout error. **]** 

**SRS_CSHARP_MQTT_TRANSPORT_18_030: [** If the response contains a success code, `SendTwinUpdateAsync` shall return success to the caller. **]**
