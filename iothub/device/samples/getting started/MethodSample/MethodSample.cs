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
        private readonly IotHubDeviceClient _hubDeviceClient;

        private class DeviceData
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
        }

        public MethodSample(IotHubDeviceClient deviceClient)
        {
            _hubDeviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
        }

        public async Task RunSampleAsync(TimeSpan sampleRunningTime)
        {
            Console.WriteLine("Press Control+C to quit the sample.");
            using var cts = new CancellationTokenSource(sampleRunningTime);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Sample execution cancellation requested; will exit.");
            };

            _hubDeviceClient.SetConnectionStatusChangeHandler(ConnectionStatusChangeHandler);

            // Method Call processing will be enabled when the first method handler is added.
            // Setup a callback for the 'WriteToConsole' method.
            await _hubDeviceClient.SetMethodHandlerAsync(WriteToConsoleAsync, null, cts.Token);

            // Setup a callback for the 'GetDeviceName' method.
            await _hubDeviceClient.SetMethodHandlerAsync(
                GetDeviceNameAsync,
                new DeviceData { Name = "DeviceClientMethodSample" },
                cts.Token);

            var timer = Stopwatch.StartNew();
            Console.WriteLine($"Use the IoT hub Azure Portal to call methods GetDeviceName or WriteToConsole within this time.");

            Console.WriteLine($"Waiting up to {sampleRunningTime} for IoT Hub method calls ...");
            while (!cts.IsCancellationRequested
                && (sampleRunningTime == Timeout.InfiniteTimeSpan || timer.Elapsed < sampleRunningTime))
            {
                await Task.Delay(1000);
            }

            // You can unsubscribe from receiving a callback for direct methods by setting a null callback handler.
            await _hubDeviceClient.SetMethodHandlerAsync(
                "GetDeviceName",
                null,
                null);

            await _hubDeviceClient.SetMethodHandlerAsync(
                "WriteToConsole",
                null,
                null);
        }

        private void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            Console.WriteLine($"\nConnection status changed to {status}.");
            Console.WriteLine($"Connection status changed reason is {reason}.\n");
        }

        private Task<DirectMethodResponse> WriteToConsoleAsync(DirectMethodResponse methodRequest, object userContext)
        {
            Console.WriteLine($"\t *** {methodRequest.Name} was called.");
            Console.WriteLine($"\t{methodRequest.DataAsJson}\n");

            return Task.FromResult(new DirectMethodResponse(new byte[0], 200));
        }

        private Task<DirectMethodResponse> GetDeviceNameAsync(DirectMethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t *** {methodRequest.Name} was called.");

            var retValue = new DirectMethodResponse();
            if (userContext == null)
            {
                retValue.Payload = Array.Empty<byte>();
                retValue.Status = 500;
            }
            else
            {
                var deviceData = (DeviceData)userContext;
                string result = JsonSerializer.Serialize(deviceData);
                retValue.Payload = Encoding.UTF8.GetBytes(result);
                retValue.Status = 200;
            }

            return Task.FromResult(retValue);
        }
    }
}
