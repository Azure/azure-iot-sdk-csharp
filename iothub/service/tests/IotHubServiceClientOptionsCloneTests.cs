// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using FluentAssertions;
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
                Proxy = new WebProxy("localhost"),
                HttpClient = new HttpClient(new HttpClientHandler(), true),
                Protocol = IotHubTransportProtocol.WebSocket,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                CertificateRevocationCheck  = true,
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
                AmqpConnectionKeepAlive = TimeSpan.FromSeconds(1),
            };

            var clone = options.Clone();
            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);
            options.Proxy.Should().BeEquivalentTo(clone.Proxy);
            options.HttpClient.Should().BeEquivalentTo(clone.HttpClient);
            options.Protocol.Should().NotBeSameAs(clone.Protocol);
            options.Protocol.Should().BeEquivalentTo(clone.Protocol);
            options.SslProtocols.Should().NotBeSameAs(clone.SslProtocols);
            options.SslProtocols.Should().BeEquivalentTo(clone.SslProtocols);
            options.CertificateRevocationCheck.Should().BeTrue();
            clone.CertificateRevocationCheck.Should().BeTrue();
            options.SdkAssignsMessageId.Should().NotBeSameAs(clone.SdkAssignsMessageId);
            options.SdkAssignsMessageId.Should().BeEquivalentTo(clone.SdkAssignsMessageId);
            options.AmqpConnectionKeepAlive.Should().Be(TimeSpan.FromSeconds(1));
            clone.AmqpConnectionKeepAlive.Should().Be(TimeSpan.FromSeconds(1));

            clone.Protocol = IotHubTransportProtocol.Tcp;
            options.Should().NotBeEquivalentTo(clone);
        }
    }
}
