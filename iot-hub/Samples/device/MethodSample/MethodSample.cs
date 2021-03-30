// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;

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

        public async Task RunSampleAsync(TimeSpan sampleRunningTime)
        {
            _deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);

            // Method Call processing will be enabled when the first method handler is added.
            // Setup a callback for the 'WriteToConsole' method.
            await _deviceClient.SetMethodHandlerAsync("WriteToConsole", WriteToConsoleAsync, null);

            // Setup a callback for the 'GetDeviceName' method.
            await _deviceClient.SetMethodHandlerAsync(
                "GetDeviceName",
                GetDeviceNameAsync,
                new DeviceData { Name = "DeviceClientMethodSample" });

            Console.WriteLine("Press Control+C to quit the sample.");
            using var cts = new CancellationTokenSource(sampleRunningTime);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Sample execution cancellation requested; will exit.");
            };

            var timer = Stopwatch.StartNew();
            Console.WriteLine($"Use the IoT Hub Azure Portal to call methods GetDeviceName or WriteToConsole within this time.");

            Console.WriteLine($"Waiting up to {sampleRunningTime} for IoT Hub method calls ...");
            while (!cts.IsCancellationRequested
                && timer.Elapsed < sampleRunningTime)
            {
                await Task.Delay(1000);
            }

            // You can unsubscribe from receiving a callback for direct methods by setting a null callback handler.
            await _deviceClient.SetMethodHandlerAsync(
                "GetDeviceName",
                null,
                null);

            await _deviceClient.SetMethodHandlerAsync(
                "WriteToConsole",
                null,
                null);
        }

        private void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            Console.WriteLine($"\nConnection status changed to {status}.");
            Console.WriteLine($"Connection status changed reason is {reason}.\n");
        }

        private Task<MethodResponse> WriteToConsoleAsync(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t *** {methodRequest.Name} was called.");
            Console.WriteLine($"\t{methodRequest.DataAsJson}\n");

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
                var deviceData = (DeviceData)userContext;
                string result = JsonSerializer.Serialize(deviceData);
                retValue = new MethodResponse(Encoding.UTF8.GetBytes(result), 200);
            }

            return Task.FromResult(retValue);
        }
    }
}
