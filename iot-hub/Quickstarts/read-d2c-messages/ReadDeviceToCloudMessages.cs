// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure Event Hubs Client for .NET
// For samples see: https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventhub/Azure.Messaging.EventHubs/samples/README.md
// For documentation see: https://docs.microsoft.com/azure/event-hubs/

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;

namespace read_d2c_messages
{
    public class ReadDeviceToCloudMessages
    {
        // Event Hub-compatible endpoint
        // az iot hub show --query properties.eventHubEndpoints.events.endpoint --name {your IoT Hub name}
        private const string EventHubsCompatibleEndpoint = "{your Event Hubs compatible endpoint}";

        // Event Hub-compatible name
        // az iot hub show --query properties.eventHubEndpoints.events.path --name {your IoT Hub name}
        private const string EventHubName = "{your Event Hubs compatible name}";

        // az iot hub policy show --name service --query primaryKey --hub-name {your IoT Hub name}
        private const string IotHubSasKeyName = "service";
        private const string IotHubSasKey = "{your service primary key}";

        // Asynchronously create a PartitionReceiver for a partition and then start
        // reading any messages sent from the simulated client.
        private static async Task ReceiveMessagesFromDeviceAsync(CancellationToken cancellationToken)
        {
            // If you chose to copy the "Event Hub-compatible endpoint" from the "Built-in endpoints" section
            // of your IoT Hub instance in the Azure portal, you can set the connection string to that value
            // directly and remove the call to "BuildEventHubsConnectionString".
            string connectionString = BuildEventHubsConnectionString(EventHubsCompatibleEndpoint, IotHubSasKeyName, IotHubSasKey);

            // Create the consumer using the default consumer group using a direct connection to the service.
            // Information on using the client with a proxy can be found in the README for this quick start, here:
            //   https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Quickstarts/read-d2c-messages/README.md#websocket-and-proxy-support
            //
            await using EventHubConsumerClient consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, connectionString, EventHubName);

            Console.WriteLine("Listening for messages on all partitions");

            try
            {
                // Begin reading events for all partitions, starting with the first event in each partition and waiting indefinitely for
                // events to become available.  Reading can be canceled by breaking out of the loop when an event is processed or by
                // signaling the cancellation token.
                //
                // The "ReadEventsAsync" method on the consumer is a good starting point for consuming events for prototypes
                // and samples.  For real-world production scenarios, it is strongly recommended that you consider using the
                // "EventProcessorClient" from the "Azure.Messaging.EventHubs.Processor" package.
                //
                // More information on the "EventProcessorClient" and its benefits can be found here:
                //   https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventhub/Azure.Messaging.EventHubs.Processor/README.md
                //
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync(cancellationToken))
                {
                    Console.WriteLine("Message received on partition {0}:", partitionEvent.Partition.PartitionId);

                    string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                    Console.WriteLine("\t{0}:", data);

                    Console.WriteLine("Application properties (set by device):");
                    foreach (var prop in partitionEvent.Data.Properties)
                    {
                        Console.WriteLine("\t{0}: {1}", prop.Key, prop.Value);
                    }

                    Console.WriteLine("System properties (set by IoT Hub):");
                    foreach (var prop in partitionEvent.Data.SystemProperties)
                    {
                        Console.WriteLine("\t{0}: {1}", prop.Key, prop.Value);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // This is expected when the token is signaled; it should not be considered an
                // error in this scenario.
            }
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("IoT Hub Quickstarts - Read device to cloud messages. Ctrl-C to exit.\n");
            using var cancellationSource = new CancellationTokenSource();

            void cancelKeyPressHandler(object sender, ConsoleCancelEventArgs eventArgs)
            {
                eventArgs.Cancel = true;
                cancellationSource.Cancel();
                Console.WriteLine("Exiting...");

                Console.CancelKeyPress -= cancelKeyPressHandler;
            }

            Console.CancelKeyPress += cancelKeyPressHandler;
            await ReceiveMessagesFromDeviceAsync(cancellationSource.Token);
        }

        private static string BuildEventHubsConnectionString(string eventHubsEndpoint,
                                                             string iotHubSharedKeyName,
                                                             string iotHubSharedKey) =>
            $"Endpoint={ eventHubsEndpoint };SharedAccessKeyName={ iotHubSharedKeyName };SharedAccessKey={ iotHubSharedKey }";

    }
}
