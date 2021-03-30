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

        public async Task RunSampleAsync(TimeSpan sampleRunningTime)
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

            Console.WriteLine("Press Control+C to quit the sample.");
            using var cts = new CancellationTokenSource(sampleRunningTime);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Cancellation requested; will exit.");
            };

            var timer = Stopwatch.StartNew();
            Console.WriteLine($"Use the IoT Hub Azure Portal or IoT Explorer utility to change the twin desired properties.");

            Console.WriteLine($"Waiting up to {sampleRunningTime} for receiving twin desired property updates ...");
            while (!cts.IsCancellationRequested
                && timer.Elapsed < sampleRunningTime)
            {
                await Task.Delay(1000);
            }

            // This is how one can unsubscribe a callback for properties using a null callback handler.
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(null, null);
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
