// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningTransportRegisterRequestTests
    {
        private readonly string _globalDeviceEndpoint = "endpoint";
        private readonly string _idScope = "id";

        [TestMethod]
        public void ProvisioningTransportRegisterRequest_DefaultPayload()
        {
            // arrange - act
            var auth = new Mock<AuthenticationProvider>();

            var request = new ProvisioningTransportRegisterRequest(
                _globalDeviceEndpoint,
                _idScope,
                auth.Object);

            // assert
            request.GlobalDeviceEndpoint.Should().Be(_globalDeviceEndpoint);
            request.IdScope.Should().Be(_idScope);
            request.Payload.Should().BeNull();
        }

        [TestMethod]
        public void ProvisioningTransportRegisterRequest_WithPayload()
        {
            // arrange - act
            var auth = new Mock<AuthenticationProvider>();

            var request = new ProvisioningTransportRegisterRequest(
                _globalDeviceEndpoint, 
                _idScope,
                auth.Object,
                new RegistrationRequestPayload());

            // assert
            request.GlobalDeviceEndpoint.Should().Be(_globalDeviceEndpoint);
            request.IdScope.Should().Be(_idScope);
            request.Payload.Should().BeEquivalentTo(new RegistrationRequestPayload());
        }
    }
}
