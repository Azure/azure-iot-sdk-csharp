// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// This sample demonstrates the two options available for receiving messages sent to a device client instance.
    /// You can receive messages either by calling the polling ReceiveAsync() API, or by setting callback to receive messages using SetReceiveMessageHandlerAsync().
    /// If you set a callback for receiving messages, any subsequent calls to the polling ReceiveAsync() API will return null.
    /// Setting a callback for receiving messages removes the need for you to continuously poll for received messages.
    /// It is worth noting that the callback is available only over Mqtt, Mqtt_WebSocket_Only, Mqtt_Tcp_Only, Amqp, Amqp_WebSocket_Only, Amqp_Tcp_only.
    /// Http1 does not support setting callbacks since internally we would need to poll for received messages anyway.
    /// </summary>
    public class MessageReceiveSample
    {
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(3);
        private readonly TimeSpan? _maxRunTime;
        private readonly DeviceClient _deviceClient;

        public MessageReceiveSample(DeviceClient deviceClient, TimeSpan? maxRunTime)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
            _maxRunTime = maxRunTime;
        }

        public async Task RunSampleAsync()
        {
            using var cts = _maxRunTime.HasValue
                ? new CancellationTokenSource(_maxRunTime.Value)
                : new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Sample execution cancellation requested; will exit.");
            };
            Console.WriteLine($"{DateTime.Now}> Press Control+C at any time to quit the sample.");

            // First receive C2D messages using the polling ReceiveAsync().
            Console.WriteLine($"\n{DateTime.Now}> Device waiting for C2D messages from the hub...");
            Console.WriteLine($"{DateTime.Now}> Use the Azure Portal IoT hub blade or Azure IoT Explorer to send a message to this device.");
            await ReceiveC2dMessagesPollingAndComplete(cts.Token);

            // Now subscribe to receive C2D messages through a callback.
            await _deviceClient.SetReceiveMessageHandlerAsync(OnC2dMessageReceived, _deviceClient);
            Console.WriteLine($"\n{DateTime.Now}> Subscribed to receive C2D messages over callback.");

            // Now wait to receive C2D messages through the callback.
            // Since you are subscribed to receive messages through the callback, any call to the polling ReceiveAsync() API will now return "null".
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
            await _deviceClient.SetReceiveMessageHandlerAsync(null, null);
        }

        private async Task ReceiveC2dMessagesPollingAndComplete(CancellationToken ct)
        {
            Console.WriteLine($"{DateTime.Now}> Trying to receive C2D messages by polling using the ReceiveAsync() method. Press 'n' to move to the next phase.");

            while (!ct.IsCancellationRequested)
            {
                if (Console.IsInputRedirected // the pipeline doesn't have a console or redirects console input
                    || (Console.KeyAvailable
                        && ConsoleKey.N == Console.ReadKey().Key))
                {
                    Console.WriteLine($"\n{DateTime.Now}> Ending message polling.");
                    break;
                }

                using Message receivedMessage = await _deviceClient.ReceiveAsync(s_sleepDuration);
                if (receivedMessage == null)
                {
                    continue;
                }

                Console.WriteLine($"{DateTime.Now}> Polling using ReceiveAsync() - received message with Id={receivedMessage.MessageId}");
                PrintMessage(receivedMessage);

                await _deviceClient.CompleteAsync(receivedMessage);
                Console.WriteLine($"{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");
            }
        }

        private async Task OnC2dMessageReceived(Message receivedMessage, object _)
        {
            Console.WriteLine($"{DateTime.Now}> C2D message callback - message received with Id={receivedMessage.MessageId}.");
            PrintMessage(receivedMessage);

            await _deviceClient.CompleteAsync(receivedMessage);
            Console.WriteLine($"{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");

            receivedMessage.Dispose();
        }

        private void PrintMessage(Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            var formattedMessage = new StringBuilder($"Received message: [{messageData}]\n");

            // User set application properties can be retrieved from the Message.Properties dictionary.
            foreach (KeyValuePair<string, string> prop in receivedMessage.Properties)
            {
                formattedMessage.AppendLine($"\tProperty: key={prop.Key}, value={prop.Value}");
            }
            // System properties can be accessed using their respective accessors, e.g. DeliveryCount.
            formattedMessage.AppendLine($"\tDelivery count: {receivedMessage.DeliveryCount}");

            Console.WriteLine($"{DateTime.Now}> {formattedMessage}");
        }
    }
}
