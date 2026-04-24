// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class DeviceD2CNoRetry : DeviceClientScenario
    {
        private Task _sendTask;
        private Task _waitForDisconnectTask;


        public DeviceD2CNoRetry(PerfScenarioConfig config) : base(config)
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
            Task completedTask = await Task.WhenAny(_waitForDisconnectTask, _sendTask).ConfigureAwait(false);

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
            if (_sendTask == null || _sendTask.IsCompleted) _sendTask = SendMessageAsync(ct);
        }

        public override Task TeardownAsync(CancellationToken ct)
        {
            return CloseAsync(ct);
        }
    }
}
