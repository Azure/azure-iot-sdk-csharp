// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class DeviceC2DNoRetry : DeviceClientScenario
    {
        private Task _receiveTask;
        private Task _waitForDisconnectTask;


        public DeviceC2DNoRetry(PerfScenarioConfig config) : base(config)
        {
        }

        public override async Task SetupAsync(CancellationToken ct)
        {
            await CreateDeviceAsync().ConfigureAwait(false);
            DisableRetry();
            await OpenDeviceAsync(ct).ConfigureAwait(false);
        }

        public override async Task RunTestAsync(CancellationToken ct)
        {
            SetupTasks(ct);
            Task completedTask = await Task.WhenAny(_waitForDisconnectTask, _receiveTask).ConfigureAwait(false);

            if (completedTask == _waitForDisconnectTask)
            {
                DisposeDevice();
                await SetupAsync(ct).ConfigureAwait(false);
                SetupTasks(ct);
            }
        }

        private void SetupTasks(CancellationToken ct)
        {
            if (_waitForDisconnectTask == null || _waitForDisconnectTask.IsCompleted) _waitForDisconnectTask = WaitForDisconnectedAsync(ct);
            if (_receiveTask == null || _receiveTask.IsCompleted) _receiveTask = ReceiveMessageAsync(ct);
        }

        public override Task TeardownAsync(CancellationToken ct)
        {
            return CloseAsync(ct);
        }
    }
}
