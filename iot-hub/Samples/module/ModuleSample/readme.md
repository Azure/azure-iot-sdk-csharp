# Module Sample

This sample illustrates how a module client handles its connection status updates, while sending telemetry to IoT Hub, and sending and receiving twin property updates.

### Initialize the client:

```csharp
// Connection string:
// Get the module connection string from Azure IoT Portal, or using Azure CLI. 
// Azure portal - 
// Navigate to your IoT Hub. From the left pane, under "Explorers", click on "IoT devices".
// Click and navigate to your device. Under "Module Identities", click on your module.
// Copy the connection strings listed (primary and/or secondary).
// Azure CLI - 
//  az iot hub module-identity connection-string show --device-id --module-id <module_id> [--key-type {primary, secondary}]
//  --key-type is optional. It defaults to "primary".
//
// Transport type:
// The transport to use to communicate with the IoT Hub. Possible values include Mqtt,
// Mqtt_WebSocket_Only, Mqtt_Tcp_Only, Amqp, Amqp_WebSocket_Only, Amqp_Tcp_only, and Http1.
//
// Pass them to the application using command line parameters (see Parameters.cs).

string connectionString = "<connection_string>";
TransportType transportType = TransportType.Mqtt;
moduleClient = ModuleClient.CreateFromConnectionString(connectionString, transportType);
```

### Send telemetry:

```csharp
var temperature = 25;
var humidity = 70;
string messagePayload = $"{{\"temperature\":{temperature},\"humidity\":{humidity}}}";

using var eventMessage = new Message(Encoding.UTF8.GetBytes(messagePayload))
{
    MessageId = Guid.NewGuid().ToString(),
    ContentEncoding = Encoding.UTF8.ToString(),
    ContentType = "application/json",
};

await moduleClient.SendEventAsync(message);
```


### Receive desired property updates from the IoT Hub service, and send reported property update from the device:

```csharp
private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
{
    Console.WriteLine("Desired property changed:");
    Console.WriteLine($"\t{desiredProperties.ToJson()}");

    Console.WriteLine("Sending current time as reported property");
    TwinCollection reportedProperties = new TwinCollection();
    reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;

    await moduleClient.UpdateReportedPropertiesAsync(reportedProperties);
}

await moduleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, moduleClient);
```


### Module reconnection:

This sample illustrates the various connection status changes and connection status change reasons the module client can return, and how to handle them.

Some examples on how to simulate client reconnection:
- Unplugging the network cable - this will cause a transient network exception to be thrown, which will be retried internally by the SDK.
- Roll over your client instance's shared access key or initialize your client instance with a shared access signature based connection string (with a fixed expiration time for the token) - this will cause the client to return a status of `Disconnected` with a status change reason of `Bad_Credential`. If you perform an operation when the client is in this state, your application will receive an `UnauthorizedException`, which is marked as non-transient. The SDK will not retry in this case.
<br/>You will need to dispose the existing client instance, update your keys and reinitialize a new client instance.
- You can disable your module instance or delete it from your IoT hub instance - this will cause the client to return a status of `Disconnected` with a status change reason of `Device_Disabled`. If you perform an operation when the client is in this state, your application will receive a `DeviceNotFoundException`, which is marked as non-transient. The SDK will not retry in this case.
<br/>You will need to fix your module's status in Azure, and then reinitialize a new client instance.

The module client exhibits the following connection status changes with reason:

<table>
  <tr>
    <th> Connection Status </th>
    <th> Change Reason </th>
    <th> Ownership of connectivity </th>
    <th> Comments </th>
    <th> Action </th>
  </tr>
  <tr>
    <td> Connected </td>
    <td> Connection_Ok </td>
    <td> SDK </td>
    <td> SDK tries to remain connected to the service and can carry out all operations as normal. </td>
    <td> The client is ready to be used. </td>
  </tr>
  <tr>
    <td> Disconnected_Retrying </td>
    <td> Communication_Error </td>
    <td> SDK </td>
    <td> When disconnection happens because of any reason (network failures, transient loss of connectivity etc.), SDK makes best attempt to connect back to IotHub. The RetryPolicy applied on the ModuleClient will be used to determine the count of reconnection attempts for <em>retriable</em> errors. </td>
    <td> Do NOT dispose and reinitialize the client when it is in this state. <br/> Any operation carried out will be queued up, and will be subsequently either completed (if client reports a state of "Connected") or abandoned (if the client reports a state of "Disconnected"). </td>
  </tr>
  <tr>
    <td rowspan="4"> Disconnected </td>
    <td> Device_Disabled </td>
    <td rowspan="4"> Application </td>
    <td> This signifies that the module has been deleted or marked as disabled (on your hub instance). </td>
    <td> Dispose the existing client instance, fix the module status in Azure and then reinitialize a new client instance. </td>
  </tr>
  <tr>
    <td> Bad_Credential </td>
    <td> Supplied credential isn’t good for module to connect to service. </td>
    <td> Dispose the existing client instance, fix the supplied credentials and then reinitialize a new client instance. </td>
  </tr>
  <tr>
    <td> Communication_Error </td>
    <td> This is the state when SDK landed up in a non-retriable error during communication. </td>
    <td> If you want to perform more operations on the module client, you should inspect the associated exception details to determine if user intervention is required. <br/> Dispose the existing client instance, make modifications (if required), and then reinitialize a new client instance. </td>
  </tr>
  <tr>
    <td> Retry_Expired </td>
    <td> This signifies that the client was disconnected due to a transient exception, but the retry policy expired before a connection could be re-established. </td>
    <td> If you want to perform more operations on the module client, you should dispose and then re-initialize the client. </td>
  </tr>
  <tr>
    <td> Disabled </td>
    <td> Client_Close </td>
    <td> Application </td>
    <td> This is the state when SDK was asked to close the connection by application. </td>
    <td> If you want to perform more operations on the module client, you should dispose and then re-initialize the client. </td>
  </tr>
</table>

NOTE:
* If the module is in `Connected` state, you can perform subsequent operations on the same client instance.
* If the module is in `Disconnected_Retrying` state, then the SDK is retrying to recover its connection. Wait until module recovers and reports a `Connected` state, and then perform subsequent operations.
* If the module is in `Disconnected` or `Disabled` state, then the underlying transport layer has been disposed. You should dispose of the existing `ModuleClient` instance and then initialize a new client (initializing a new `ModuleClient` instance without disposing the previously used instance will cause them to fight for the same connection resources).
