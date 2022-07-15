# Service Client Sample

This sample illustrates how to send cloud-to-device messages using an IoT Hub service client instance. This sample also demonstrates how to handle recoverable and non-recoverable exceptions.

### Initialize the client:

```csharp
// Connection string:
// Get the IoT Hub connection string from Azure IoT Portal, or using Azure CLI. 
// Azure portal - 
// Navigate to your IoT Hub. From the left pane, under "Settings", click on "Shared access policies".
// For sending c2d messages to a device you will need to connect with either "service" or "iothubowner" policy permissions.
// Click on the selected policy and copy the connection string listed (primary or secondary).
// Azure CLI - 
//  az iot hub show-connection-string [--name <iot-hub-name>] [--policy-name <{service/iothubowner}>] [--key <{primary/secondary}>]
//
// Transport type:
// The transport to use to communicate with the IoT Hub. Possible values include Amqp and Amqp_WebSocket_Only.
//
// Pass them to the application using command line parameters (see Parameters.cs).

string connectionString = "<connection_string>";
TransportType transportType = TransportType.Amqp;
serviceClient = ServiceClient.CreateFromConnectionString(connectionString, transportType);
```

### Send cloud-to-device telemetry:

```csharp
string deviceId = "<device_id>";
var temperature = 25;
var humidity = 70;
string messagePayload = $"{{\"temperature\":{temperature},\"humidity\":{humidity}}}";

using var eventMessage = new Message(Encoding.UTF8.GetBytes(messagePayload))
{
    MessageId = Guid.NewGuid().ToString(),
    ContentEncoding = Encoding.UTF8.ToString(),
    ContentType = "application/json",
};

await serviceClient.SendEventAsync(deviceId, message);
```


### Service client reconnection:

The service client does not have any kind of connection recovery logic built in. On encountering an exception, the service client will relay that information to the calling application. 
At that point, it is recommended that you inspect the exception details and take necessary action.

For eg.:
- If it a network exception, you can retry the operation.
- If it is a security exception (unauthorized exception), inspect your credentials and make sure they are up-to-date.
- If it is a throttling/quota exceeded exception, monitor and/or modify the frequency of sending requests, or update your hub instance scale unit. See https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-quotas-throttling for more details.