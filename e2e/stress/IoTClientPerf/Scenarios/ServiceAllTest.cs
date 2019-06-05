// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class ServiceAllTest : ServiceClientScenario
    {
        private Task _sendTask;
        private Task _callMethodTask;

        public ServiceAllTest(PerfScenarioConfig config) : base(config)
        {
            CreateServiceClient();
        }

        public override async Task SetupAsync(CancellationToken ct)
        {
            await OpenServiceClientAsync(ct).ConfigureAwait(false);
        }

        public override Task RunTestAsync(CancellationToken ct)
        {
            if (_sendTask == null || _sendTask.IsCompleted) _sendTask = SendMessageAsync(ct);
            if (_callMethodTask == null || _callMethodTask.IsCompleted) _callMethodTask = CallMethodAsync(ct);

            return Task.WhenAny(_sendTask, _callMethodTask);
        }

        public override Task TeardownAsync(CancellationToken ct)
        {
            return CloseAsync(ct);
        }
    }
}
