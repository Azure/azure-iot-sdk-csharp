// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class MessageSample
    {
        private const int MessageCount = 5;
        private const int TemperatureThreshold = 30;
        private static Random s_randomGenerator = new Random();
        private float _temperature;
        private float _humidity;
        private ModuleClient _moduleClient;

        public MessageSample(ModuleClient moduleClient)
        {
            _moduleClient = moduleClient ?? throw new ArgumentNullException(nameof(moduleClient));
        }

        public async Task RunSampleAsync()
        {
            await SendEvent().ConfigureAwait(false);
        }

        private async Task SendEvent()
        {
            string dataBuffer;

            Console.WriteLine("Module sending {0} messages to IoTHub...\n", MessageCount);

            for (int count = 0; count < MessageCount; count++)
            {
                _temperature = s_randomGenerator.Next(20, 35);
                _humidity = s_randomGenerator.Next(60, 80);
                dataBuffer = $"{{\"messageId\":{count},\"temperature\":{_temperature},\"humidity\":{_humidity}}}";
                Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                eventMessage.Properties.Add("temperatureAlert", (_temperature > TemperatureThreshold) ? "true" : "false");
                Console.WriteLine("\t{0}> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), count, dataBuffer);

                await _moduleClient.SendEventAsync(eventMessage).ConfigureAwait(false);
            }
        }
    }
}
