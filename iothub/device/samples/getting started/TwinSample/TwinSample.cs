﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

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
            Console.WriteLine("Press Control+C to quit the sample.");
            using var cts = new CancellationTokenSource(sampleRunningTime);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Cancellation requested; will exit.");
            };

            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChangedAsync, null);

            Console.WriteLine("Retrieving twin...");
            Twin twin = await _deviceClient.GetTwinAsync();

            Console.WriteLine("\tInitial twin value received:");
            Console.WriteLine($"\t{twin.ToJson()}");

            Console.WriteLine("Sending sample start time as reported property");
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastAppLaunch"] = DateTime.UtcNow;

            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);

            var timer = Stopwatch.StartNew();
            Console.WriteLine($"Use the IoT Hub Azure Portal or IoT Explorer utility to change the twin desired properties.");

            Console.WriteLine($"Waiting up to {sampleRunningTime} for receiving twin desired property updates ...");
            while (!cts.IsCancellationRequested
                && (sampleRunningTime == Timeout.InfiniteTimeSpan || timer.Elapsed < sampleRunningTime))
            {
                await Task.Delay(1000);
            }

            // This is how one can unsubscribe a callback for properties using a null callback handler.
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(null, null);
        }

        private async Task OnDesiredPropertyChangedAsync(TwinCollection desiredProperties, object userContext)
        {
            var reportedProperties = new TwinCollection();

            Console.WriteLine("\tDesired properties requested:");
            Console.WriteLine($"\t{desiredProperties.ToJson()}");

            // For the purpose of this sample, we'll blindly accept all twin property write requests.
            foreach (KeyValuePair<string, object> desiredProperty in desiredProperties)
            {
                Console.WriteLine($"Setting {desiredProperty.Key} to {desiredProperty.Value}.");
                reportedProperties[desiredProperty.Key] = desiredProperty.Value;
            }

            Console.WriteLine("\tAlso setting current time as reported property");
            reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.UtcNow;

            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }
    }
}
