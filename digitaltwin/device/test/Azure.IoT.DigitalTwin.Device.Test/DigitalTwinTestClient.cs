// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.Iot.DigitalTwin.Device;
using Azure.Iot.DigitalTwin.Device.Model;
using Microsoft.Azure.Devices.Client;

namespace Azure.IoT.DigitalTwin.Device.Test
{
    internal class DigitalTwinTestClient : DigitalTwinClient
    {
        public DigitalTwinTestClient(DeviceClient deviceClient)
            : base(deviceClient)
        {
        }

        internal new async Task ReportPropertiesAsync(string instanceName, IEnumerable<DigitalTwinPropertyReport> properties, CancellationToken cancellationToken)
        {
            Console.WriteLine("called");
        }
    }
}
