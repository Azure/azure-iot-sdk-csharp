// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class MessageSample
    {
        private static readonly Random s_randomGenerator = new Random();
        private readonly DeviceClient _deviceClient;

        public MessageSample(DeviceClient deviceClient)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
        }

        public async Task RunSampleAsync()
        {
            await SendEventAsync();
            await ReceiveMessagesAsync();
        }

        private async Task SendEventAsync()
        {
            const int MessageCount = 5;
            Console.WriteLine($"Device sending {MessageCount} messages to IoT Hub...\n");

            float temperature;
            float humidity;

            for (int count = 0; count < MessageCount; count++)
            {
                temperature = s_randomGenerator.Next(20, 35);
                humidity = s_randomGenerator.Next(60, 80);

                string dataBuffer = $"{{\"messageId\":{count},\"temperature\":{temperature},\"humidity\":{humidity}}}";

                using var eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer))
                {
                    ContentType = "application/json",
                    ContentEncoding = Encoding.UTF8.ToString(),
                };

                const int TemperatureThreshold = 30;
                bool tempAlert = temperature > TemperatureThreshold;
                eventMessage.Properties.Add("temperatureAlert", tempAlert.ToString());
                Console.WriteLine($"\t{DateTime.Now}> Sending message: {count}, data: [{dataBuffer}]");

                await _deviceClient.SendEventAsync(eventMessage);
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            Console.WriteLine("\nDevice waiting for C2D messages from the hub...");
            Console.WriteLine("Use the Azure Portal IoT Hub blade or Azure IoT Explorer to send a message to this device.");

            using Message receivedMessage = await _deviceClient.ReceiveAsync(TimeSpan.FromSeconds(30));
            if (receivedMessage == null)
            {
                Console.WriteLine($"\t{DateTime.Now}> Timed out");
                return;
            }

            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            Console.WriteLine($"\t{DateTime.Now}> Received message: {messageData}");

            int propCount = 0;
            foreach (var prop in receivedMessage.Properties)
            {
                Console.WriteLine($"\t\tProperty[{propCount++}> Key={prop.Key} : Value={prop.Value}");
            }

            await _deviceClient.CompleteAsync(receivedMessage);
        }
    }
}
