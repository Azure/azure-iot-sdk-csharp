// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure Event Hubs Client for .NET
// For samples see: https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventhub/Azure.Messaging.EventHubs/samples/README.md
// For documentation see: https://docs.microsoft.com/azure/event-hubs/

using Azure.Messaging.EventHubs.Consumer;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReadD2cMessages
{
    /// <summary>
    /// A sample to illustrate reading Device-to-Cloud messages from a service app.
    /// </summary>
    internal class Program
    {
        private static Parameters _parameters;

        public static async Task Main(string[] args)
        {
            // Parse application parameters
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    _parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            // Either the connection string must be supplied, or the set of endpoint, name, and shared access key must be.
            if (string.IsNullOrWhiteSpace(_parameters.EventHubConnectionString)
                && (string.IsNullOrWhiteSpace(_parameters.EventHubCompatibleEndpoint)
                    || string.IsNullOrWhiteSpace(_parameters.EventHubName)
                    || string.IsNullOrWhiteSpace(_parameters.SharedAccessKey)))
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            Console.WriteLine("IoT Hub Quickstarts - Read device to cloud messages. Ctrl-C to exit.\n");

            // Set up a way for the user to gracefully shutdown
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            // Run the sample
            await ReceiveMessagesFromDeviceAsync(cts.Token);

            Console.WriteLine("Cloud message reader finished.");
        }

        // Asynchronously create a PartitionReceiver for a partition and then start
        // reading any messages sent from the simulated client.
        private static async Task ReceiveMessagesFromDeviceAsync(CancellationToken ct)
        {
            string connectionString = _parameters.GetEventHubConnectionString();

            // Create the consumer using the default consumer group using a direct connection to the service.
            // Information on using the client with a proxy can be found in the README for this quick start, here:
            // https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Quickstarts/ReadD2cMessages/README.md#websocket-and-proxy-support
            await using var consumer = new EventHubConsumerClient(
                EventHubConsumerClient.DefaultConsumerGroupName,
                connectionString,
                _parameters.EventHubName);

            Console.WriteLine("Listening for messages on all partitions.");

            try
            {
                // Begin reading events for all partitions, starting with the first event in each partition and waiting indefinitely for
                // events to become available. Reading can be canceled by breaking out of the loop when an event is processed or by
                // signaling the cancellation token.
                //
                // The "ReadEventsAsync" method on the consumer is a good starting point for consuming events for prototypes
                // and samples. For real-world production scenarios, it is strongly recommended that you consider using the
                // "EventProcessorClient" from the "Azure.Messaging.EventHubs.Processor" package.
                //
                // More information on the "EventProcessorClient" and its benefits can be found here:
                //   https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventhub/Azure.Messaging.EventHubs.Processor/README.md
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync(ct))
                {
                    Console.WriteLine($"\nMessage received on partition {partitionEvent.Partition.PartitionId}:");

                    string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                    Console.WriteLine($"\tMessage body: {data}");

                    Console.WriteLine("\tApplication properties (set by device):");
                    foreach (KeyValuePair<string, object> prop in partitionEvent.Data.Properties)
                    {
                        PrintProperties(prop);
                    }

                    Console.WriteLine("\tSystem properties (set by IoT Hub):");
                    foreach (KeyValuePair<string, object> prop in partitionEvent.Data.SystemProperties)
                    {
                        PrintProperties(prop);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // This is expected when the token is signaled; it should not be considered an
                // error in this scenario.
            }
        }

        private static void PrintProperties(KeyValuePair<string, object> prop) 
        {
            string propValue = prop.Value is DateTime
                ? ((DateTime)prop.Value).ToString("O") // using a built-in date format here that includes milliseconds
                : prop.Value.ToString();

            Console.WriteLine($"\t\t{prop.Key}: {propValue}");
        }
    }
}
