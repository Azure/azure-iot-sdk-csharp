# Read device-to-cloud messages

This sample demonstrates how to use the Azure Event Hubs client library for .NET to read messages sent from a device by using the built-in Event Hub that exists by default for every IoT Hub instance.

## Prerequisites

The .NET SDK 3.1 is recommended. You can download the .NET Core SDK for multiple platforms from [.NET](https://www.microsoft.com/net/download/all). You can verify the current version of C# on your development machine using 'dotnet --version'.

> Note: the Event Hubs client 5.2 does not work with .NET 5.0.

## Obtain the Event Hub-compatible connection string

You can get the Event Hub-compatible connection string to your Iot Hub instance via the Azure portal or by using the Azure CLI.

If using the Azure portal, see [Built in endpoints for IotHub](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-read-builtin#read-from-the-built-in-endpoint) to get the Event Hubs-compatible connection string and assign it to the variable `connectionString` in the sample. You can skip the Azure CLI instructions in the sample after this.

If using the Azure CLI, you will need to run the below before running this sample to get the details required to form the Event Hubs compatible connection string:

```bash
az iot hub show --query properties.eventHubEndpoints.events.endpoint --name {your IoT Hub name}
az iot hub show --query properties.eventHubEndpoints.events.path --name {your IoT Hub name}
az iot hub policy show --name service --query primaryKey --hub-name {your IoT Hub name}
```

If you can do neither of the above and need to programmatically get this information, the sample [How to request the IoT Hub built-in Event Hubs-compatible endpoint connection string](https://github.com/Azure/azure-sdk-for-net/blob/master/samples/iothub-connect-to-eventhubs/README.md) demonstrates how to do so.

## WebSocket and proxy support

If you would like to use WebSockts, with our without a proxy, you will need to create a set of options for the [`EventHubConsumerClient`](https://docs.microsoft.com/en-us/dotnet/api/azure.messaging.eventhubs.consumer.eventhubconsumerclient?view=azure-dotnet) to configure its behavior.  Proxy support is offered via the [`IWebProxy`](https://docs.microsoft.com/dotnet/api/system.net.iwebproxy?view=netcore-3.1) interface, which includes the built-in [`WebProxy`](https://docs.microsoft.com/dotnet/api/system.net.webproxy?view=netcore-3.1) class.  Any proxy must be explicitly passed; the client does not assume that any proxy set via the ambient environment or system-wide is desired.

The options may be created as follows:

```csharp
var options = new EventHubConsumerClientOptions();

// This line sets the transport to use WebSockets.
options.ConnectionOptions.TransportType = EventHubsTransportType.AmqpWebSockets;

// The following lines configure the options for proxy use.
IWebProxy proxy = new WebProxy("<< URI TO PROXY >>", true);
options.ConnectionOptions.Proxy = proxy;
```

Once you have your options, you'll need to pass them to the client constructor. Each constructor accepts a set of options as the last parameter, such as:

```csharp
string consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;
await using var consumer = new EventHubConsumerClient(consumerGroup, "<< CONNECTION STRING >>", "<< EVENT HUB >>", options);
```

## Additional Resources

- [Event Hubs Product Documentation](https://docs.microsoft.com/azure/event-hubs/)
- [Event Hubs Client Library Documentation](https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/eventhub/Azure.Messaging.EventHubs/README.md)
- [Event Hubs Samples](https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventhub/Azure.Messaging.EventHubs/samples/README.md)
- [Event Processor Client Library Documentation](https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/eventhub/Azure.Messaging.EventHubs.Processor/README.md)
- [Event Processor Samples](https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventhub/Azure.Messaging.EventHubs.Processor/samples/README.md)