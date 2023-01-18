﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// This sample demonstrates how to send and receive messages on an Azure edge module.
    /// </summary>
    /// <remarks>
    /// For simplicity, this sample sends telemetry messages to the module itself.
    /// </remarks>
    public class EdgeModuleMessageSample
    {
        private readonly TimeSpan? _maxRunTime;
        private readonly string _outputTarget;
        private readonly IotHubModuleClient _moduleClient;

        public EdgeModuleMessageSample(IotHubModuleClient moduleClient, string outputTarget, TimeSpan? maxRunTime)
        {
            _moduleClient = moduleClient ?? throw new ArgumentNullException(nameof(moduleClient));
            _outputTarget = outputTarget;
            _maxRunTime = maxRunTime;
        }

        public async Task RunSampleAsync()
        {
            using CancellationTokenSource cts = _maxRunTime.HasValue
                ? new CancellationTokenSource(_maxRunTime.Value)
                : new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Sample execution cancellation requested; will exit.");
            };
            Console.WriteLine($"{DateTime.Now}> Press Control+C at any time to quit the sample.");

            _moduleClient.ConnectionStatusChangeCallback = OnConnectionStatusChanged;
            await _moduleClient.OpenAsync(cts.Token).ConfigureAwait(false);

            // Now setting a callback for receiving a message from the module queue.
            await _moduleClient.SetIncomingMessageCallbackAsync(PrintMessage).ConfigureAwait(false);
            Console.WriteLine($"\n{DateTime.Now}> Subscribed to receive module messages over callback.");

            // Now wait to receive module messages through the callback.
            Console.WriteLine($"\n{DateTime.Now}> Module waiting to receive messages from the hub...");

            // Now sending message to the module itself.
            var message = new TelemetryMessage(Encoding.ASCII.GetBytes("sample message"));
            await _moduleClient.SendTelemetryAsync("toNextMod", message).ConfigureAwait(false);
            Console.WriteLine($"\n{DateTime.Now}> Sent telemetry message to the module.");

            // Now continue to send messages to the module with every key press of 'M'.
            while (!cts.IsCancellationRequested)
            {
                if (Console.ReadKey().Key == ConsoleKey.M)
                {
                    message = new TelemetryMessage(Encoding.ASCII.GetBytes("sample message"));
                    await _moduleClient.SendTelemetryAsync(_outputTarget, message).ConfigureAwait(false);
                }
            }

            // Now disposing module client.
            await _moduleClient.DisposeAsync().ConfigureAwait(false);
        }

        private static void OnConnectionStatusChanged(ConnectionStatusInfo connectionStatusInfo)
        {
            Console.WriteLine($"\nConnection status changed to {connectionStatusInfo.Status}.");
            Console.WriteLine($"Connection status changed reason is {connectionStatusInfo.ChangeReason}.\n");
        }

        private static Task<MessageAcknowledgement> PrintMessage(IncomingMessage receivedMessage)
        {
            bool messageDeserialized = receivedMessage.TryGetPayload(out string messageData);

            if (messageDeserialized)
            {
                var formattedMessage = new StringBuilder($"Received message: [{messageData}]\n");

                // User set application properties can be retrieved from the Message.Properties dictionary.
                foreach (KeyValuePair<string, string> prop in receivedMessage.Properties)
                {
                    formattedMessage.AppendLine($"\tProperty: key={prop.Key}, value={prop.Value}");
                }

                Console.WriteLine($"{DateTime.Now}> {formattedMessage}");
            }
            else
            {
                Console.WriteLine($"Could not deserialize the received message. Please check your serializer settings.");
            }
            return Task.FromResult(MessageAcknowledgement.Complete);
        }
    }
}
