// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("FaultInjection")]
    [TestCategory("IoTHub-Client")]
    [TestCategory("PoolAmqp")]
    [TestCategory("LongRunning")]
    public partial class FaultInjectionPoolAmqpTests : E2EMsTestBase
    {
        private static readonly string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;
    }
}
