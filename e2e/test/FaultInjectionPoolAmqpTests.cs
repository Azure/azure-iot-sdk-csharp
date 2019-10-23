// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    [TestCategory("IoTHub-FaultInjection-PoolAmqp")]
    public partial class FaultInjectionPoolAmqpTests : IDisposable
    {
        private static TestLogging _log = TestLogging.GetInstance();
        private readonly ConsoleEventListener _listener;

        public FaultInjectionPoolAmqpTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
