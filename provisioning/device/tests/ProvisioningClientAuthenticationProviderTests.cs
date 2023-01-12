using Microsoft.VisualStudio.TestTools.UnitTesting;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningClientAuthenticationProviderTests
    {
        [TestMethod]
        public void AuthenticationProviderX509_ThrowsWhenMissingCert()
        {
            Assert.ThrowsException<ArgumentException>(() => new AuthenticationProviderX509(null));
        }
    }
}