// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class DeviceD2CTest : DeviceClientScenario
    {
        public DeviceD2CTest(PerfScenarioConfig config) : base(config)
        {
        }

        public override async Task SetupAsync(CancellationToken ct)
        {
            await CreateDeviceAsync().ConfigureAwait(false);
            await OpenDeviceAsync(ct).ConfigureAwait(false);
        }

        public override Task RunTestAsync(CancellationToken ct)
        {
            return SendMessageAsync(ct);
        }

        public override Task TeardownAsync(CancellationToken ct)
        {
            return CloseAsync(ct);
        }
    }
}
