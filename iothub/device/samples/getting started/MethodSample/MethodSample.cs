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

            // Setup a callback dispatcher for the incoming methods.
            await _hubDeviceClient.SetMethodHandlerAsync(
                OnDirectMethodCalledAsync,
                null,
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
                null,
                null);
        }

        private async Task<DirectMethodResponse> OnDirectMethodCalledAsync(DirectMethodRequest directMethodRequest, object userContext)
        {
            switch (directMethodRequest.MethodName)
            {
                case "GetDeviceName":
                    return await GetDeviceNameAsync(directMethodRequest, userContext);

                case "WriteToConsole":
                    return await WriteToConsoleAsync(directMethodRequest, userContext);

                default:
                    return new DirectMethodResponse
                    {
                        Status = 400,
                    };
            }
        }

        private void ConnectionStatusChangeHandler(ConnectionStatusInfo connectionInfo)
        {
            Console.WriteLine($"\nConnection status changed to {connectionInfo.Status}.");
            Console.WriteLine($"Connection status changed reason is {connectionInfo.ChangeReason}.\n");
        }

        private Task<DirectMethodResponse> WriteToConsoleAsync(DirectMethodRequest directMethodRequest, object userContext)
        {
            Console.WriteLine($"\t *** {directMethodRequest.MethodName} was called.");
            Console.WriteLine($"\t{directMethodRequest.PayloadAsJsonString}\n");

            var directMethodResponse = new DirectMethodResponse();
            directMethodResponse.Status = 200;

            return Task.FromResult(directMethodResponse);
        }

        private Task<DirectMethodResponse> GetDeviceNameAsync(DirectMethodRequest directMethodRequest, object userContext)
        {
            Console.WriteLine($"\t *** {directMethodRequest.MethodName} was called.");

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
