// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class ServiceMethodTest : ServiceClientScenario
    {
        public ServiceMethodTest(PerfScenarioConfig config) : base(config)
        {
            CreateServiceClient();
        }

        public override async Task SetupAsync(CancellationToken ct)
        {
            await OpenServiceClientAsync(ct).ConfigureAwait(false);
        }

        public override Task RunTestAsync(CancellationToken ct)
        {
            return CallMethodAsync(ct);
        }

        public override Task TeardownAsync(CancellationToken ct)
        {
            return CloseAsync(ct);
        }
    }
}
