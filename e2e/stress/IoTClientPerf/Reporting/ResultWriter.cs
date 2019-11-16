// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public abstract class ResultWriter
    {
        protected string _header;

        public ResultWriter(string header = null)
        {
            _header = header;
        }

        public Task WriteAsync(TelemetryMetrics m)
        {
            return WriteLineAsync(m.ToString());
        }

        public abstract Task FlushAsync();

        protected abstract Task WriteLineAsync(string s);
    }
}
