// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.E2ETests
{
    public struct PerfScenarioConfig
    {
        public int Id;
        public ResultWriter Writer;
        public int SizeBytes;
        public string AuthType;
        public Client.TransportType Transport;
        public int PoolSize;
    }
}
