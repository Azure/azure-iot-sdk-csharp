// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("FaultInjection")]
    [TestCategory("PoolAmqp")]
    [TestCategory("LongRunning")]
    public partial class FaultInjectionPoolAmqpTests : IDisposable
    {
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener = TestConfig.StartEventListener();

        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}
