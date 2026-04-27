// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class DeviceAllTest : DeviceClientScenario
    {
        private Task _sendTask;
        private Task _receiveTask;
        private Task _waitForMethodTask;


        public DeviceAllTest(PerfScenarioConfig config) : base(config)
        {
        }

        public override async Task SetupAsync(CancellationToken ct)
        {
            await CreateDeviceAsync().ConfigureAwait(false);
            await OpenDeviceAsync(ct).ConfigureAwait(false);
            await EnableMethodsAsync(ct).ConfigureAwait(false);
        }

        public override Task RunTestAsync(CancellationToken ct)
        {
            if (_sendTask == null || _sendTask.IsCompleted) _sendTask = SendMessageAsync(ct);
            if (_receiveTask == null || _receiveTask.IsCompleted) _receiveTask = ReceiveMessageAsync(ct);
            if (_waitForMethodTask == null || _waitForMethodTask.IsCompleted) _waitForMethodTask = WaitForMethodAsync(ct);

            return Task.WhenAny(_sendTask, _receiveTask, _waitForMethodTask);
        }

        public override Task TeardownAsync(CancellationToken ct)
        {
            return CloseAsync(ct);
        }
    }
}
