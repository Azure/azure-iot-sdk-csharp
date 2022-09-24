// Copyright (c) Microsoft. All rights reserved.
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
    /// You can receive messages by setting callback to receive messages using SetReceiveMessageHandlerAsync().
    /// </summary>
    public class MessageReceiveSample
    {
        private readonly TimeSpan? _maxRunTime;
        private readonly IotHubDeviceClient _deviceClient;
        private readonly Transport _transport;

        public MessageReceiveSample(IotHubDeviceClient deviceClient, Transport transportType, TimeSpan? maxRunTime)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
            _transport = transportType;
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

            // Now subscribe to receive C2D messages through a callback (which isn't supported over HTTP).
            await _deviceClient.SetReceiveMessageHandlerAsync(OnC2dMessageReceivedAsync, _deviceClient);
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

        private Task<MessageAcknowledgement> OnC2dMessageReceivedAsync(Message receivedMessage, object _)
        {
            Console.WriteLine($"{DateTime.Now}> C2D message callback - message received with Id={receivedMessage.MessageId}.");
            PrintMessage(receivedMessage);

            Console.WriteLine($"{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");
            return Task.FromResult(MessageAcknowledgement.Complete);
        }

        private static void PrintMessage(Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.Payload);
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
