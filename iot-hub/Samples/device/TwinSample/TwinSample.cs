// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class TwinSample
    {
        private readonly DeviceClient _deviceClient;

        public TwinSample(DeviceClient deviceClient)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
        }

        public async Task RunSampleAsync()
        {
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChangedAsync, null);

            Console.WriteLine("Retrieving twin...");
            Twin twin = await _deviceClient.GetTwinAsync();

            Console.WriteLine("\tInitial twin value received:");
            Console.WriteLine($"\t{twin.ToJson()}");

            Console.WriteLine("Sending sample start time as reported property");
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastAppLaunch"] = DateTime.UtcNow;

            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Sample execution cancellation requested; will exit.");
            };

            var waitTime = TimeSpan.FromMinutes(5);
            var timer = Stopwatch.StartNew();

            Console.WriteLine($"Use the IoT Hub Azure Portal to change the Twin desired properties within this time.");
            Console.WriteLine($"Waiting up to {waitTime} for IoT Hub Twin updates...");
            while (!cts.IsCancellationRequested
                && timer.Elapsed < waitTime)
            {
                await Task.Delay(1000);
            }
        }

        private async Task OnDesiredPropertyChangedAsync(TwinCollection desiredProperties, object userContext)
        {
            Console.WriteLine("\tDesired property changed:");
            Console.WriteLine($"\t{desiredProperties.ToJson()}");

            Console.WriteLine("\tSending current time as reported property");
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.UtcNow;

            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }
    }
}
