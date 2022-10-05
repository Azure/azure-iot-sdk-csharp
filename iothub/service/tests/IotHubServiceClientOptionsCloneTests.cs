﻿// Copyright (c) Microsoft. All rights reserved.
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
            // arrange
            var options = new IotHubServiceClientOptions
            {
                Proxy = new WebProxy("localhost"),
                HttpClient = new HttpClient(new HttpClientHandler(), true),
                Protocol = IotHubTransportProtocol.WebSocket,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                CertificateRevocationCheck = true,
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
                AmqpConnectionKeepAlive = TimeSpan.FromSeconds(1),
            };

            // act
            var clone = options.Clone();

            // asssert
            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);

            // change one property to validate 'NotEquivalent' works
            clone.Protocol = IotHubTransportProtocol.Tcp;
            options.Should().NotBeEquivalentTo(clone);
        }
    }
}