// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public abstract class PerfScenario
    {
        protected ResultWriter _writer;
        protected int _sizeBytes;
        protected string _authType;
        protected Client.TransportType _transport;
        protected int _id;

        protected PerfScenario(PerfScenarioConfig config)
        {
            _writer = config.Writer;
            _sizeBytes = config.SizeBytes;
            _authType = config.AuthType;
            _transport = config.Transport;
            _id = config.Id;
        }

        public abstract Task SetupAsync(CancellationToken ct);

        public abstract Task RunTestAsync(CancellationToken ct);

        public abstract Task TeardownAsync(CancellationToken ct);
    }
}
