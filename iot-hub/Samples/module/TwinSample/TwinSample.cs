// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class TwinSample
    {
        private ModuleClient _moduleClient;

        public TwinSample(ModuleClient moduleClient)
        {
            _moduleClient = moduleClient ?? throw new ArgumentNullException(nameof(moduleClient));
        }

        public async Task RunSampleAsync()
        {
            await _moduleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, null).ConfigureAwait(false);

            Console.WriteLine("Retrieving twin...");
            Twin twin = await _moduleClient.GetTwinAsync().ConfigureAwait(false);

            Console.WriteLine("\tInitial twin value received:");
            Console.WriteLine($"\t{twin.ToJson()}");

            Console.WriteLine("Sending sample start time as reported property");
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;

            await _moduleClient.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);

            Console.WriteLine("Waiting 30 seconds for IoT Hub Twin updates...");
            Console.WriteLine($"Use the IoT Hub Azure Portal to change the Twin desired properties within this time.");

            await Task.Delay(30 * 1000);
        }

        private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            Console.WriteLine("\tDesired property changed:");
            Console.WriteLine($"\t{desiredProperties.ToJson()}");

            Console.WriteLine("\tSending current time as reported property");
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;

            await _moduleClient.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
        }
    }
}
