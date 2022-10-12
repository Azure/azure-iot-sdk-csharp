﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// This sample demonstrates how to receive cloud-to-device messages sent to a device client instance.
    /// You can receive messages by setting callback to receive messages using SetMessageHandlerAsync().
    /// </summary>
    public class MessageReceiveSample
    {
        private readonly TimeSpan? _maxRunTime;
        private readonly IotHubDeviceClient _deviceClient;

        public MessageReceiveSample(IotHubDeviceClient deviceClient, TimeSpan? maxRunTime)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
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

            await _deviceClient.OpenAsync(cts.Token);

            // Now subscribe to receive C2D messages through a callback (which isn't supported over HTTP).
            await _deviceClient.SetMessageCallbackAsync(OnC2dMessageReceivedAsync);
            Console.WriteLine($"\n{DateTime.Now}> Subscribed to receive C2D messages over callback.");

            // Now wait to receive C2D messages through the callback.
            Console.WriteLine($"\n{DateTime.Now}> Device waiting for C2D messages from the hub...");
            Console.WriteLine($"{DateTime.Now}> Use the Azure Portal IoT Hub blade or Azure IoT Explorer to send a message to this device.");

            try
            {
                await Task.Delay(-1, cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Done running.
            }

            // Now unsubscibe from receiving the callback.
            await _deviceClient.SetMessageCallbackAsync(null);
        }

        private Task<MessageAcknowledgement> OnC2dMessageReceivedAsync(IncomingMessage receivedMessage)
        {
            Console.WriteLine($"{DateTime.Now}> C2D message callback - message received with Id={receivedMessage.MessageId}.");
            PrintMessage(receivedMessage);

            Console.WriteLine($"{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");
            return Task.FromResult(MessageAcknowledgement.Complete);
        }

        private static void PrintMessage(IncomingMessage receivedMessage)
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
        }
    }
}
