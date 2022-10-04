// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubDeviceClientOptionsCloneTest
    {
        [TestMethod]
        public void IotHubClientMqttSettings()
        {
            var settings = new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)
            {
                IdleTimeout = TimeSpan.FromSeconds(1),
            };
            var options = new IotHubClientOptions(settings)
            {
                GatewayHostName = "sampleHost",
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
            };
            var clone = options.Clone();

            Assert.AreNotSame(options, clone);
        }

        [TestMethod]
        public void IotHubClientAmqpSettings()
        {
            var settings = new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket)
            {
                IdleTimeout = TimeSpan.FromSeconds(1),
            };
            var options = new IotHubClientOptions(settings)
            {
                GatewayHostName = "sampleHost",
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
            };
            var clone = options.Clone();

            Assert.AreNotSame(options, clone);
        }
    }
}
