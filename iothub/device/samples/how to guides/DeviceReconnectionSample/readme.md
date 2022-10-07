# Device Reconnection Sample

This sample code demonstrates the various connection status changes and connection status change reasons the device client can return, and how to handle them.

### Initialize the client:

```csharp
// Connection string:
// Get the device connection string from Azure IoT Portal, or using Azure CLI. 
// Azure portal - 
// Navigate to your IoT Hub. From the left pane, under "Explorers", click on "IoT devices".
// Click and navigate to your device.
// Copy the connection strings listed (primary and/or secondary).
// Azure CLI - 
//  az iot hub device-identity connection-string show --device-id <device_id> [--key-type {primary, secondary}]
//  --key-type is optional. It defaults to "primary".
//
// Transport:
// The transport to use to communicate with IoT ub. Possible values include Mqtt and Amqp.
//
// Transport protocol:
// The protocol to use to communicate with IoT hub. Possible values include Tcp and WebSocket.

string connectionString = "<connection_string>";

// This option is helpful in delegating the assignment of Message.MessageId to the sdk.
// If the user doesn't set a value for Message.MessageId, the sdk will assign it a random GUID before sending the message.
var options = new IotHubClientOptions(new IotHubClientMqttSettings())
{
    SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
};
deviceClient = new IotHubDeviceClient(connectionString, options);
```

### Send device to cloud telemetry:

```csharp
// This snippet shows you how to call the API for sending telemetry from your device client.
// In order to ensure that your client is resilient to disconnection events and exceptions, refer to https://github.com/Azure-Samples/azure-iot-samples-csharp/blob/main/iot-hub/Samples/device/DeviceReconnectionSample/DeviceReconnectionSample.cs.
var temperature = 25;
var humidity = 70;
string messagePayload = $"{{\"temperature\":{temperature},\"humidity\":{humidity}}}";

var eventMessage = new Message(Encoding.UTF8.GetBytes(messagePayload))
{
    ContentEncoding = Encoding.UTF8.ToString(),
    ContentType = "application/json",
};

await deviceClient.SendEventAsync(message);
```

### Receive cloud to device telemetry (using the callback) and complete the message:

```csharp
// This snippet shows you how to call the API for receiving telemetry sent to your device client.
// In order to ensure that your client is resilient to disconnection events and exceptions,
// refer to https://github.com/Azure-Samples/azure-iot-samples-csharp/blob/main/iot-hub/Samples/device/DeviceReconnectionSample/DeviceReconnectionSample.cs.
private async Task OnC2dMessageReceived(IncomingMessage receivedMessage)
{
    bool messageDeserialized = receivedMessage.TryGetPayload(out string messageData);
    if (messageDeserialized)
    {
        var formattedMessage = new StringBuilder($"Received message: [{messageData}]");

        foreach (KeyValuePair<string, string> prop in receivedMessage.Properties)
        {
            formattedMessage.AppendLine($"\n\tProperty: key={prop.Key}, value={prop.Value}");
        }
        _logger.LogInformation(formattedMessage.ToString());

        _logger.LogInformation($"Completed message [{messageData}].");
        return Task.FromResult(MessageAcknowledgement.Complete);
    }

    // A message was received that did not conform to the serialization specifications; ignore it.
    return Task.FromResult(MessageAcknowledgement.Reject);
}
```

### Receive twin desired property update notifications and update device twin's reported properties:

```csharp
// This snippet shows you how to call the APIs for receiving twin desired property update notifications sent to your device client
// and sending reported property updates from your device client.
// In order to ensure that your client is resilient to disconnection events and exceptions,
// refer to https://github.com/Azure-Samples/azure-iot-samples-csharp/blob/main/iot-hub/Samples/device/DeviceReconnectionSample/DeviceReconnectionSample.cs.
private async Task HandleTwinUpdateNotificationsAsync(TwinCollection twinUpdateRequest)
{
    _logger.LogInformation($"Twin property update requested: \n{twinUpdateRequest.ToJson()}");

    // For the purpose of this sample, we'll blindly accept all twin property write requests.
    var reportedProperties = new TwinCollection();
    foreach (KeyValuePair<string, object> desiredProperty in twinUpdateRequest)
    {
        _logger.LogInformation($"Setting property {desiredProperty.Key} to {desiredProperty.Value}.");
        reportedProperties[desiredProperty.Key] = desiredProperty.Value;
    }

    // The device app usually responds to a twin desired property update notification by sending a reported property update.
    await deviceClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);
}

// Subscribe to the twin desired property update API.
await deviceClient.SetDesiredPropertyUpdateCallbackAsync(HandleTwinUpdateNotificationsAsync, userContext);

// Once you are done receiving twin desired property update notifications sent to your device client,
// you can unsubscribe from the callback by setting a null handler.
await deviceClient.SetDesiredPropertyUpdateCallbackAsync(null, null);
```

### Connection status change behavior:

Some examples on how to simulate client reconnection:
- Unplugging the network cable - this will cause a transient network exception to be thrown, which will be retried internally by the SDK.
- Roll over your client instance's shared access key or initialize your client instance with a shared access signature based connection string (with a fixed expiration time for the token) - this will cause the client to return a status of `Disconnected` with a status change reason of `Bad_Credential`. If you perform an operation when the client is in this state, your application will receive an `UnauthorizedException`, which is marked as non-transient. The SDK will not retry in this case.
<br/>You will need to dispose the existing client instance, update your keys and reinitialize a new client instance.
- You can disable your device instance or delete it from your IoT hub instance - this will cause the client to return a status of `Disconnected` with a status change reason of `Device_Disabled`. If you perform an operation when the client is in this state, your application will receive a `DeviceNotFoundException`, which is marked as non-transient. The SDK will not retry in this case.
<br/>You will need to fix your device's status in Azure, and then reinitialize a new client instance.
- You can also see the client reconnection in action during sas token renewal on MQTT. Sas token renewals on MQTT as not proactive, but are instead reliant on the service disconnecting the connection on token expiry (there is a ~10mins delay, during which service continues to accept the expired token). Once service disconnects the connection, the sdk will report a status of `Disconnected_Retrying` with a status change reason of `Communication_Error`, and will then immediately attempt to reconnect with a renewed sas token.
<br/>The disconnection on token expiration should not be more than a couple of seconds. If this disconnection is not acceptable for your application, you should consider using AMQP protocol instead, which will proactively renew the sas token prior to its expiration.

The device client exhibits the following connection status changes with reason:

<table>
  <tr>
    <th>Connection Status</th>
    <th>Change Reason</th>
    <th>Ownership of connectivity</th>
    <th>Comments</th>
    <th>Action</th>
  </tr>
  <tr>
    <td>Connected</td>
    <td>ConnectionOk</td>
    <td>SDK</td>
    <td>SDK tries to remain connected to the service and can carry out all operations as normal.</td>
    <td>The client is ready to be used.</td>
  </tr>
  <tr>
    <td>DisconnectedRetrying</td>
    <td>CommunicationError</td>
    <td>SDK</td>
    <td>When disconnection happens because of any reason (network failures, transient loss of connectivity etc.), SDK makes best attempt to connect back to IotHub. The RetryPolicy applied on the DeviceClient will be used to determine the count of reconnection attempts for <em>retriable</em> errors.</td>
    <td>Do NOT dispose and reinitialize the client when it is in this state. <br/> Any operation carried out will be queued up, and will be subsequently either completed (if client reports a state of "Connected") or abandoned (if the client reports a state of "Disconnected").</td>
  </tr>
  <tr>
    <td rowspan="4">Disconnected</td>
    <td>DeviceDisabled</td>
    <td rowspan="4">Application</td>
    <td>This signifies that the device/module has been deleted or marked as disabled (on your hub instance).</td>
    <td>Dispose the existing client instance, fix the device/module status in Azure and then reinitialize a new client instance.</td>
  </tr>
  <tr>
    <td>BadCredential</td>
    <td>Supplied credential isn’t good for device to connect to service.</td>
    <td>Dispose the existing client instance, fix the supplied credentials and then reinitialize a new client instance.</td>
  </tr>
  <tr>
    <td>CommunicationError</td>
    <td>This is the state when SDK landed up in a non-retriable error during communication.</td>
    <td>If you want to perform more operations on the device client, you should inspect the associated exception details to determine if user intervention is required. <br/> Dispose the existing client instance, make modifications (if required), and then reinitialize a new client instance.</td>
  </tr>
  <tr>
    <td>RetryExpired</td>
    <td>This signifies that the client was disconnected due to a transient exception, but the retry policy expired before a connection could be re-established.</td>
    <td>If you want to perform more operations on the device client, you should dispose and then re-initialize the client. </br> Note that the SDK's default retry policy is set to never expire.</td>
  </tr>
  <tr>
    <td>Disabled</td>
    <td>ClientClosed</td>
    <td>Application</td>
    <td>This is the state when SDK was asked to close the connection by application.</td>
    <td>If you want to perform more operations on the device client, you should dispose and then re-initialize the client.</td>
  </tr>
</table>

NOTE:
- If the device is in `Connected` state, you can perform subsequent operations on the same client instance.
- If the device is in `DisconnectedRetrying` state, then the SDK is retrying to recover its connection. Wait until device recovers and reports a `Connected` state, and then perform subsequent operations.
- If the device is in `Disconnected` or `Disabled` state, then the underlying transport layer has been disposed. You should dispose of the existing `DeviceClient` instance and then initialize a new client (initializing a new `DeviceClient` instance without disposing the previously used instance will cause them to fight for the same connection resources).
