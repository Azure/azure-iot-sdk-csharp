// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Authentication;
using FluentAssertions;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("UnitTest")]
    public class ProvisioningTransportHandlerAmqpTests
    {
        private static readonly string s_notTransientErrorDescription = "{\"errorCode\":403101, \"trackingId\":\"fake-tracking-id-A\", \"message\":\"fake-error-message-A\", \"info\":null}";
        private static readonly string s_transientErrorDescription = "{\"errorCode\":429101, \"trackingId\":\"fake-tracking-id-B\", \"message\":\"fake-error-message-B\", \"info\":null}";
        private static readonly string s_invalidErrorDescription = "fake-invalid-error-description";

        [TestMethod]
        public void ProvisioningTransportHandlerAmqp_ValidateOutcome_NotTransientError()
        {
            // arrange

            var rejected= new Rejected() { Error = new Error() { Description = s_notTransientErrorDescription } };

            var options = new ProvisioningClientOptions(new ProvisioningClientAmqpSettings());
            var transportHandler = new ProvisioningTransportHandlerAmqp(options);

            // act
            Action act = () => transportHandler.ValidateOutcome(rejected);

            // assert

            var error = act.Should().Throw<ProvisioningClientException>();
            error.And.ErrorCode.Should().Be(403101);
            error.And.TrackingId.Should().Be("fake-tracking-id-A");
            error.And.Message.Should().Be("fake-error-message-A");
            error.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        public void ProvisioningTransportHandlerAmqp_ValidateOutcome_TransientError()
        {
            // arrange

            var rejected = new Rejected() { Error = new Error() { Description = s_transientErrorDescription } };

            var options = new ProvisioningClientOptions(new ProvisioningClientAmqpSettings());
            var transportHandler = new ProvisioningTransportHandlerAmqp(options);

            // act
            Action act = () => transportHandler.ValidateOutcome(rejected);

            // assert

            var error = act.Should().Throw<ProvisioningClientException>();
            error.And.ErrorCode.Should().Be(429101);
            error.And.TrackingId.Should().Be("fake-tracking-id-B");
            error.And.Message.Should().Be("fake-error-message-B");
            error.And.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void ProvisioningTransportHandlerAmqp_ValidateOutcome_InvalidError()
        {
            // arrange

            var rejected = new Rejected() { Error = new Error() { Description = s_invalidErrorDescription } };

            var options = new ProvisioningClientOptions(new ProvisioningClientAmqpSettings());
            var transportHandler = new ProvisioningTransportHandlerAmqp(options);

            // act
            Action act = () => transportHandler.ValidateOutcome(rejected);

            // assert

            var error = act.Should().Throw<ProvisioningClientException>();
            error.And.Message.Should().Be($"AMQP transport exception: malformed server error message: '{rejected.Error.Description}'");
            error.And.InnerException.Should().BeOfType<JsonReaderException>();
            error.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        public void ProvisioningTransportHandlerAmqp_ContainsAuthenticationException_NullException()
        {
            // arrange - act
            bool flag = ProvisioningTransportHandlerAmqp.ContainsAuthenticationException(null);

            // assert
            flag.Should().BeFalse();
        }

        [TestMethod]
        public void ProvisioningTransportHandlerAmqp_ContainsAuthenticationException_NoInnerException()
        {
            // arrange
            var ex = new Exception();

            // act
            bool flag = ProvisioningTransportHandlerAmqp.ContainsAuthenticationException(ex);

            // assert
            flag.Should().BeFalse();
        }

        [TestMethod]
        public void ProvisioningTransportHandlerAmqp_ContainsAuthenticationException_InnerAuthenticationException()
        {
            // arrange
            var ex = new Exception("", new AuthenticationException());

            // act
            bool flag = ProvisioningTransportHandlerAmqp.ContainsAuthenticationException(ex);

            // assert
            flag.Should().BeTrue();
        }
    }
}
