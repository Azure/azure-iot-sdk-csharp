// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class MethodSample
    {
        private readonly DeviceClient _deviceClient;

        private class DeviceData
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
        }

        public MethodSample(DeviceClient deviceClient)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
        }

        public async Task RunSampleAsync()
        {
            _deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);

            // Method Call processing will be enabled when the first method handler is added.
            // Setup a callback for the 'WriteToConsole' method.
            await _deviceClient
                .SetMethodHandlerAsync("WriteToConsole", WriteToConsoleAsync, null)
                .ConfigureAwait(false);

            // Setup a callback for the 'GetDeviceName' method.
            await _deviceClient
                .SetMethodHandlerAsync("GetDeviceName", GetDeviceNameAsync, new DeviceData { Name = "DeviceClientMethodSample" })
                .ConfigureAwait(false);

            Console.WriteLine("Waiting 30 seconds for IoT Hub method calls ...");

            Console.WriteLine($"Use the IoT Hub Azure Portal to call methods GetDeviceName or WriteToConsole within this time.");
            await Task.Delay(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
        }

        private void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            Console.WriteLine();
            Console.WriteLine($"Connection status changed to {status}.");
            Console.WriteLine($"Connection status changed reason is {reason}.");
            Console.WriteLine();
        }

        private Task<MethodResponse> WriteToConsoleAsync(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t *** {methodRequest.Name} was called.");

            Console.WriteLine();
            Console.WriteLine("\t{0}", methodRequest.DataAsJson);
            Console.WriteLine();

            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }

        private Task<MethodResponse> GetDeviceNameAsync(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t *** {methodRequest.Name} was called.");

            MethodResponse retValue;
            if (userContext == null)
            {
                retValue = new MethodResponse(new byte[0], 500);
            }
            else
            {
                var d = userContext as DeviceData;
                string result = JsonSerializer.Serialize(d);
                retValue = new MethodResponse(Encoding.UTF8.GetBytes(result), 200);
            }

            return Task.FromResult(retValue);
        }
    }
}
