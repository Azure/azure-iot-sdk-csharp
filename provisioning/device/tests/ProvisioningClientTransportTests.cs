// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningClientTransportTests
    {

        private readonly string _globalDeviceEndpoint = "endpoint";
        private readonly string _idScope = "id";

        [TestMethod]
        public void ProvisioningTransportRegisterRequest()
        {
            // arrange - act
            var auth = new Mock<AuthenticationProvider>();
            auth.Setup(p => p.GetRegistrationId()).Returns("registrationId");

            var request = new ProvisioningTransportRegisterRequest(
                _globalDeviceEndpoint, 
                _idScope,
                auth.Object,
                new RegistrationRequestPayload());

            // assert
            request.Payload.Should().NotBeNull();
        }
    }
}
