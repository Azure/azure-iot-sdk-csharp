// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_receiveTimeout = TimeSpan.FromSeconds(10);
        private readonly DeviceClient _deviceClient;

        public MessageReceiveSample(DeviceClient deviceClient)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
        }

        public async Task RunSampleAsync()
        {
            // First receive C2D messages using the polling ReceiveAsync().
            Console.WriteLine($"\n{DateTime.Now}> Device waiting for C2D messages from the hub for {s_receiveTimeout}...");
            Console.WriteLine($"{DateTime.Now}> Use the Azure Portal IoT Hub blade or Azure IoT Explorer to send a message to this device.");
            await ReceiveC2dMessagesPollingAndComplete(s_receiveTimeout);

            // Now subscribe to receive C2D messages through a callback.
            await _deviceClient.SetReceiveMessageHandlerAsync(OnC2dMessageReceived, _deviceClient);
            Console.WriteLine($"\n{DateTime.Now}> Subscribed to receive C2D messages over callback.");

            // Now wait to receive C2D messages through the callback.
            // Since you are subscribed to receive messages through the callback, any call to the polling ReceiveAsync() API will now return "null".
            Console.WriteLine($"\n{DateTime.Now}> Device waiting for C2D messages from the hub for {s_receiveTimeout}...");
            Console.WriteLine($"{DateTime.Now}> Use the Azure Portal IoT Hub blade or Azure IoT Explorer to send a message to this device.");
            await Task.Delay(s_receiveTimeout);

            // Now unsubscibe from receiving the callback.
            await _deviceClient.SetReceiveMessageHandlerAsync(null, _deviceClient);
        }

        private async Task ReceiveC2dMessagesPollingAndComplete(TimeSpan timeout)
        {
            var sw = new Stopwatch();
            sw.Start();

            Console.WriteLine($"{DateTime.Now}> Receiving C2D messages on the polling ReceiveAsync().");
            while (sw.Elapsed < timeout)
            {
                using Message receivedMessage = await _deviceClient.ReceiveAsync(timeout);

                if (receivedMessage == null)
                {
                    Console.WriteLine($"{DateTime.Now}> Polling ReceiveAsync() - no message received.");
                    await Task.Delay(s_sleepDuration);
                    continue;
                }

                Console.WriteLine($"{DateTime.Now}> Polling ReceiveAsync() - received message with Id={receivedMessage.MessageId}");
                ProcessReceivedMessage(receivedMessage);

                await _deviceClient.CompleteAsync(receivedMessage);
                Console.WriteLine($"{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");
            }

            sw.Stop();
        }

        private async Task OnC2dMessageReceived(Message receivedMessage, object _)
        {
            Console.WriteLine($"{DateTime.Now}> C2D message callback - message received with Id={receivedMessage.MessageId}.");
            ProcessReceivedMessage(receivedMessage);

            await _deviceClient.CompleteAsync(receivedMessage);
            Console.WriteLine($"{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");

            receivedMessage.Dispose();
        }

        private void ProcessReceivedMessage(Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            var formattedMessage = new StringBuilder($"Received message: [{messageData}]\n");

            // User set application properties can be retrieved from the Message.Properties dictionary.
            foreach (KeyValuePair<string, string> prop in receivedMessage.Properties)
            {
                formattedMessage.AppendLine($"\tProperty: key={prop.Key}, value={prop.Value}");
            }
            // System properties can be accessed using their respective accessors.
            formattedMessage.AppendLine($"\tMessageId: {receivedMessage.MessageId}");

            Console.WriteLine($"{DateTime.Now}> {formattedMessage}");
        }
    }
}
