// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            var options = new ProvisioningServiceClientOptions()
            {
                HttpClient = new System.Net.Http.HttpClient(),
            };
            var clone = options.Clone();

            Assert.AreNotSame(options, clone);
        }
    }
}
