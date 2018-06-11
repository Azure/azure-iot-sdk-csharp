// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using System;
using System.Text;
using System.Threading.Tasks;

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
                await RunSampleWithConnectionString(_connectionString1);
            }
            catch (UnauthorizedException ex)
            {
                Console.WriteLine("UnauthorizedExpception:\n" + ex.Message);
                Console.WriteLine("Assuming key roll-over. ConnectionString1 should be reconfigured on this device.");
                
                await RunSampleWithConnectionString(_connectionString2);
            }
        }
        
        private async Task RunSampleWithConnectionString(string connectionString)
        {
            string usedConnectionString = connectionString == _connectionString1 ? "PRIMARY" : "SECONDARY";
            int delaySeconds = 5;
            Console.WriteLine($"Sending one message every {delaySeconds} seconds");

            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString, _transportType);
            var testMessage = new Message(Encoding.UTF8.GetBytes("message from key rollover sample"));

            while (true)
            {
                Console.WriteLine($"\t {DateTime.Now.ToLocalTime()} Sending message [{usedConnectionString} connection string].");
                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);
                await Task.Delay(delaySeconds * 1000);
            }
        }
    }
}
