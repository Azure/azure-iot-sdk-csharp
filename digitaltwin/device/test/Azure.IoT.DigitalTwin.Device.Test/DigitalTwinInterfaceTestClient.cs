// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.Iot.DigitalTwin.Device;
using Azure.Iot.DigitalTwin.Device.Model;

namespace Azure.IoT.DigitalTwin.Device.Test
{
    internal class DigitalTwinInterfaceTestClient : DigitalTwinInterfaceClient
    {
        public DigitalTwinInterfaceTestClient(string id, string instanceName, bool isCommandEnabled, bool isPropertyUpdatedEnabled)
            : base(id, instanceName, isCommandEnabled, isPropertyUpdatedEnabled)
        {
        }

        public async Task ReportPropertiesAsync(IEnumerable<DigitalTwinPropertyReport> properties)
        {
            await base.ReportPropertiesAsync(properties).ConfigureAwait(false);
        }

        public async Task ReportPropertiesAsync(IEnumerable<DigitalTwinPropertyReport> properties, CancellationToken ct)
        {
            await base.ReportPropertiesAsync(properties, ct).ConfigureAwait(false);
        }

        public void OnRegistrationCompleted2()
        {
            OnRegistrationCompleted();
        }
    }
}
