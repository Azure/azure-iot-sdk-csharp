// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubServiceClientOptionsCloneTests
    {
        [TestMethod]
        public void IotHubServiceClientOptions()
        {
            var options = new IotHubServiceClientOptions()
            {
                Protocol = IotHubTransportProtocol.WebSocket,
            };
            var clone = options.Clone();
            Assert.AreNotSame(clone, options);

        }
    }
}
