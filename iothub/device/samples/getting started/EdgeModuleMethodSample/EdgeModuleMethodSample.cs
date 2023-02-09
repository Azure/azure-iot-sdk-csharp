// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// This sample demonstrates how to send and receive direct methods on an Azure Edge module.
    /// </summary>
    public class EdgeModuleMethodSample
    {
        private readonly IotHubModuleClient _moduleClient;
        private string _deviceId;
        private string _moduleId;
        private readonly TimeSpan? _maxRunTime;

        public EdgeModuleMethodSample(IotHubModuleClient moduleClient, string deviceId, string moduleId, TimeSpan? maxRunTime)
        {
            _moduleClient = moduleClient ?? throw new ArgumentNullException(nameof(moduleClient));
            _deviceId = deviceId;
            _moduleId = moduleId;
            _maxRunTime = maxRunTime;
        }

        public async Task RunSampleAsync()
        {
            using CancellationTokenSource cts = _maxRunTime.HasValue
                ? new CancellationTokenSource(_maxRunTime.Value)
                : new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Sample execution cancellation requested; will exit.");
            };
            Console.WriteLine($"{DateTime.Now}> Press Control+C at any time to quit the sample.");

            _moduleClient.ConnectionStatusChangeCallback = ConnectionStatusChangeHandler;
            await _moduleClient.OpenAsync(cts.Token);

            // Setup a callback dispatcher for the incoming methods.
            await _moduleClient.SetDirectMethodCallbackAsync(OnDirectMethodCalledAsync, cts.Token);

            var timer = Stopwatch.StartNew();
            Console.WriteLine($"Use the IoT hub Azure Portal to call methods GetDeviceName or WriteToConsole within this time.");

            Console.WriteLine($"Waiting up to {_maxRunTime} for IoT Hub method calls ...");
            while (!cts.IsCancellationRequested
                && (_maxRunTime == Timeout.InfiniteTimeSpan || timer.Elapsed < _maxRunTime))
            {
                await Task.Delay(1000);
            }

            // Invoking a direct method request to the module itself.
            var directMethodRequest = new DirectMethodRequest("ModuleToModule");
            await _moduleClient.InvokeMethodAsync(_deviceId, _moduleId, directMethodRequest, cts.Token);

            // You can unsubscribe from receiving a callback for direct methods by setting a null callback handler.
            await _moduleClient.SetDirectMethodCallbackAsync(null);
        }

        private async Task<DirectMethodResponse> OnDirectMethodCalledAsync(DirectMethodRequest directMethodRequest)
        {
            switch (directMethodRequest.MethodName)
            {
                case "GetDeviceName":
                    return await GetDeviceNameAsync(directMethodRequest);

                case "WriteToConsole":
                case "ModuleToModule":
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
