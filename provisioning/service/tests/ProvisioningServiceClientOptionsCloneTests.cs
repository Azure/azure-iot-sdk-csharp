// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningServiceClientOptionsCloneTests
    {
        [TestMethod]
        public void ProvisioningServiceClientOptions()
        {
            using var handler = new HttpClientHandler();
            var options = new ProvisioningServiceClientOptions()
            {
                HttpClient = new HttpClient(handler, true),
            };
            var clone = options.Clone();

            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);
            options.ProvisioningServiceHttpSettings.Should().NotBeSameAs(clone.ProvisioningServiceHttpSettings);
            options.ProvisioningServiceHttpSettings.Should().BeEquivalentTo(clone.ProvisioningServiceHttpSettings);
            options.HttpClient.Should().BeEquivalentTo(clone.HttpClient);
        }
    }
}
