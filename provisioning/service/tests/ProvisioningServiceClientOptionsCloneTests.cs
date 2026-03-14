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
            // arrange
            using var handler = new HttpClientHandler();
            var options = new ProvisioningServiceClientOptions()
            {
                HttpClient = new HttpClient(handler, true),
            };

            // act
            var clone = options.Clone();

            // assert
            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);
        }
    }
}
