// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class MethodSample
    {
        private readonly IotHubDeviceClient _deviceClient;

        private class DeviceData
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
        }

        public MethodSample(IotHubDeviceClient deviceClient)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
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

            await _deviceClient.OpenAsync(cts.Token);

            _deviceClient.SetConnectionStatusChangeCallback(ConnectionStatusChangeHandler);

            // Setup a callback dispatcher for the incoming methods.
            await _deviceClient.SetDirectMethodCallbackAsync(
                OnDirectMethodCalledAsync,
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
            await _deviceClient.SetDirectMethodCallbackAsync(
                null);
        }

        private async Task<DirectMethodResponse> OnDirectMethodCalledAsync(DirectMethodRequest directMethodRequest)
        {
            switch (directMethodRequest.MethodName)
            {
                case "GetDeviceName":
                    return await GetDeviceNameAsync(directMethodRequest);

                case "WriteToConsole":
                    return await WriteToConsoleAsync(directMethodRequest);

                default:
                    return new DirectMethodResponse(400);
            }
        }

        private void ConnectionStatusChangeHandler(ConnectionStatusInfo connectionInfo)
        {
            Console.WriteLine($"\nConnection status changed to {connectionInfo.Status}.");
            Console.WriteLine($"Connection status changed reason is {connectionInfo.ChangeReason}.\n");
        }

        private Task<DirectMethodResponse> WriteToConsoleAsync(DirectMethodRequest directMethodRequest)
        {
            Console.WriteLine($"\t *** {directMethodRequest.MethodName} was called.");
            Console.WriteLine($"\t{directMethodRequest.GetPayloadAsJsonString()}\n");

            var directMethodResponse = new DirectMethodResponse(200);

            return Task.FromResult(directMethodResponse);
        }

        private Task<DirectMethodResponse> GetDeviceNameAsync(DirectMethodRequest directMethodRequest)
        {
            Console.WriteLine($"\t *** {directMethodRequest.MethodName} was called.");

            var retValue = new DirectMethodResponse(200);
            retValue.Payload = Encoding.UTF8.GetBytes("DeviceClientMethodSample");

            return Task.FromResult(retValue);
        }
    }
}
