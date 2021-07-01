﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("FaultInjection")]
    [TestCategory("PoolAmqp")]
    [TestCategory("LongRunning")]
    public partial class FaultInjectionPoolAmqpTests : E2EMsTestBase
    {
        private static readonly string s_proxyServerAddress = TestConfiguration.IoTHub.ProxyServerAddress;
    }
}
