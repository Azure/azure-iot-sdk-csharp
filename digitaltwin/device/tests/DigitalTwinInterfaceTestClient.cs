// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.IoT.DigitalTwin.Device;
using Azure.IoT.DigitalTwin.Device.Model;

namespace Azure.IoT.DigitalTwin.Device.Test
{
    internal class DigitalTwinInterfaceTestClient : DigitalTwinInterfaceClient
    {
        public DigitalTwinInterfaceTestClient(string id, string instanceName)
            : base(id, instanceName)
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

        public async Task SendTelemetryAsync(string telemetryName, string telemetryValue)
        {
            await base.SendTelemetryAsync(telemetryName, telemetryValue).ConfigureAwait(false);
        }

        public async Task SendTelemetryAsync(string telemetryName, string telemetryValue, CancellationToken ct)
        {
            await base.SendTelemetryAsync(telemetryName, telemetryValue, ct).ConfigureAwait(false);
        }

        public async Task UpdateAsyncCommandStatusAsync(DigitalTwinAsyncCommandUpdate update)
        {
            await base.UpdateAsyncCommandStatusAsync(update).ConfigureAwait(false);
        }

        public async Task UpdateAsyncCommandStatusAsync(DigitalTwinAsyncCommandUpdate update, CancellationToken ct)
        {
            await base.UpdateAsyncCommandStatusAsync(update, ct).ConfigureAwait(false);
        }

        public void OnRegistrationCompleted2()
        {
            OnRegistrationCompleted();
        }
    }
}
