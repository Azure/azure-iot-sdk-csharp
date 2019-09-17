// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class DeviceAllNoRetry : DeviceClientScenario
    {
        private const int DelaySecondsAfterFailure = 1;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1); 
        private Task _sendTask;
        private Task _receiveTask;
        private Task _waitForMethodTask;
        private Task _waitForDisconnectTask;


        public DeviceAllNoRetry(PerfScenarioConfig config) : base(config)
        {
        }

        public override async Task SetupAsync(CancellationToken ct)
        {
            await CreateDeviceAsync().ConfigureAwait(false);
            DisableRetry();
            await OpenDeviceAsync(ct).ConfigureAwait(false);
            await EnableMethodsAsync(ct).ConfigureAwait(false);
        }

        public override async Task RunTestAsync(CancellationToken ct)
        {
            try
            {
                await _lock.WaitAsync().ConfigureAwait(false);
                SetupTasks(ct);

                Task completedTask = await Task.WhenAny(_waitForDisconnectTask, _sendTask, _receiveTask, _waitForMethodTask).ConfigureAwait(false);

                if (completedTask == _waitForDisconnectTask)
                {
                    await DisposeDevice().ConfigureAwait(false);

                    try
                    {
                        // Drain current operations. Method will not be notified in any way of the disconnect.
                        await Task.WhenAll(_sendTask, _receiveTask).ConfigureAwait(false);
                    }
                    catch (IotHubException) { }
                    catch (OperationCanceledException) { }

                    _waitForDisconnectTask = null;
                    _sendTask = null;
                    _receiveTask = null;
                    _waitForMethodTask = null;

                    await Task.Delay(DelaySecondsAfterFailure * 1000).ConfigureAwait(false);
                    await SetupAsync(ct).ConfigureAwait(false);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private void SetupTasks(CancellationToken ct)
        {
            if (_waitForDisconnectTask == null || _waitForDisconnectTask.IsCompleted) _waitForDisconnectTask = WaitForDisconnectedAsync(ct);
            if (_sendTask == null || _sendTask.IsCompleted) _sendTask = SendMessageAsync(ct);
            if (_receiveTask == null || _receiveTask.IsCompleted) _receiveTask = ReceiveMessageAsync(ct);
            if (_waitForMethodTask == null || _waitForMethodTask.IsCompleted) _waitForMethodTask = WaitForMethodAsync(ct);
        }

        public override Task TeardownAsync(CancellationToken ct)
        {
            return CloseAsync(ct);
        }
    }
}
