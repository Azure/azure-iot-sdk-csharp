// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class AuthenticationProviderX509Tests
    {
        [TestMethod]
        public void AuthenticationProviderX509_ThrowsWhenMissingCert()
        {
            // arrange - act
            Func<AuthenticationProviderX509> act = () => new AuthenticationProviderX509(null);

            // assert
            act.Should().Throw<ArgumentException>();
        }
    }
}
