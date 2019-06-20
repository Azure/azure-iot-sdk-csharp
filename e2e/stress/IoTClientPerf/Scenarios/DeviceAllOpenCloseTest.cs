// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class DeviceAllOpenCloseTest : DeviceClientScenario
    {
        private const int MaximumWaitTimeMs = 10000;

        public DeviceAllOpenCloseTest(PerfScenarioConfig config) : base(config)
        {
        }

        public override Task SetupAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public override async Task RunTestAsync(CancellationToken ct)
        {
            await CreateDeviceAsync().ConfigureAwait(false);
            await OpenDeviceAsync(ct).ConfigureAwait(false);
            await EnableMethodsAsync(ct).ConfigureAwait(false);

            using (var cts = new CancellationTokenSource(MaximumWaitTimeMs))
            {
                Task sendTask = SendMessageAsync(cts.Token);
                Task receiveTask = ReceiveMessageAsync(cts.Token);
                Task waitForMethodTask = WaitForMethodAsync(cts.Token);

                await Task.WhenAll(sendTask, receiveTask, waitForMethodTask).ConfigureAwait(false);
            }

            await CloseAsync(ct).ConfigureAwait(false);
        }

        public override Task TeardownAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
