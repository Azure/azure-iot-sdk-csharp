// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class KeyRolloverSample
    {
        private readonly string _connectionString1;
        private readonly string _connectionString2;
        private readonly TransportType _transportType;

        public KeyRolloverSample(string connectionString1, string connectionString2, TransportType transportType)
        {
            _connectionString1 = connectionString1;
            _connectionString2 = connectionString2;
            _transportType = transportType;
        }

        public async Task RunSampleAsync()
        {
            Console.WriteLine("Update device's first connection string (using the ServiceClient SDK or DeviceExplorer) while this sample is running.");

            try
            {
                await RunSampleWithConnectionStringAsync(_connectionString1);
            }
            catch (UnauthorizedException ex)
            {
                Console.WriteLine($"UnauthorizedExpception:\n{ex.Message}");
                Console.WriteLine("Assuming key rollover. The primary key should be reconfigured on this device.");

                await RunSampleWithConnectionStringAsync(_connectionString2);
            }
        }

        private async Task RunSampleWithConnectionStringAsync(string connectionString)
        {
            bool usedPrimary = connectionString == _connectionString1;
            string usedConnectionString = usedPrimary ? "PRIMARY" : "SECONDARY";

            TimeSpan loopDelay = TimeSpan.FromSeconds(5);

            // Run longer while using the primary, and just a few times after swapped over to use the secondary
            TimeSpan runTime = usedPrimary
                ? TimeSpan.FromMinutes(5)
                : TimeSpan.FromSeconds(15);
            Console.WriteLine($"Sending one message every {loopDelay}");

            Console.WriteLine($"Connecting with {_transportType}");
            using var deviceClient = DeviceClient.CreateFromConnectionString(connectionString, _transportType);

            var timer = Stopwatch.StartNew();
            int messageCount = 1;

            while (timer.Elapsed < runTime)
            {
                Console.WriteLine($"\t {DateTime.Now} Attempting to sending message {messageCount++} using [{usedConnectionString} connection string].");

                using var testMessage = new Message(Encoding.UTF8.GetBytes("message from key rollover sample"))
                {
                    ContentType = "text/plain",
                    ContentEncoding = Encoding.UTF8.ToString(),
                };
                await deviceClient.SendEventAsync(testMessage);

                await Task.Delay(loopDelay);
            }

            timer.Stop();
        }
    }
}
