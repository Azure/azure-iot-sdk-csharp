// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Authentication;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningTransportHandlerMqttTests
    {
        [TestMethod]
        public void ProvisioningTransportHandlerMqtt_ContainsAuthenticationException_NullException()
        {
            // arrange - act
            bool flag = ProvisioningTransportHandlerMqtt.ContainsAuthenticationException(null);

            // assert
            flag.Should().BeFalse();
        }

        [TestMethod]
        public void ProvisioningTransportHandlerMqtt_ContainsAuthenticationException_NoInnerException()
        {
            // act
            bool flag = ProvisioningTransportHandlerMqtt.ContainsAuthenticationException(new Exception());

            // assert
            flag.Should().BeFalse();
        }

        [TestMethod]
        public void ProvisioningTransportHandlerMqtt_ContainsAuthenticationException_InnerAuthenticationException()
        {
            // arrange
            var ex = new Exception("", new AuthenticationException());

            // act
            bool flag = ProvisioningTransportHandlerMqtt.ContainsAuthenticationException(ex);

            // assert
            flag.Should().BeTrue();
        }
    }
}
