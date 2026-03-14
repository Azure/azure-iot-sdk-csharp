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
    [TestCategory("Unit")]
    public class ProvisioningTransportHandlerAmqpTests
    {
        private const string InvalidErrorDescription = "fake-invalid-error-description";

        private static readonly TestErrorDescription s_notTransientErrorDescription = new()
        {
            ErrorCode= 403101,
            TrackingId = "fake-tracking-id-A",
            Message= "fake-error-message-A",
        };

        private static readonly TestErrorDescription s_transientErrorDescription = new()
        {
            ErrorCode = 429101,
            TrackingId = "fake-tracking-id-B",
            Message = "fake-error-message-B",
        };

        [TestMethod]
        public void ProvisioningTransportHandlerAmqp_ValidateOutcome_NotTransientError()
        {
            // arrange

            string errorDescriptionJsonString = JsonConvert.SerializeObject(s_notTransientErrorDescription);

            var rejected= new Rejected() { Error = new Error() { Description = errorDescriptionJsonString } };

            var options = new ProvisioningClientOptions(new ProvisioningClientAmqpSettings());
            var transportHandler = new ProvisioningTransportHandlerAmqp(options);

            // act
            Action act = () => transportHandler.ValidateOutcome(rejected);

            // assert

            var error = act.Should().Throw<ProvisioningClientException>();
            error.And.ErrorCode.Should().Be(s_notTransientErrorDescription.ErrorCode);
            error.And.TrackingId.Should().Be(s_notTransientErrorDescription.TrackingId);
            error.And.Message.Should().Be(s_notTransientErrorDescription.Message);
            error.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        public void ProvisioningTransportHandlerAmqp_ValidateOutcome_TransientError()
        {
            // arrange

            string errorDescriptionJsonString = JsonConvert.SerializeObject(s_transientErrorDescription);

            var rejected = new Rejected() { Error = new Error() { Description = errorDescriptionJsonString } };

            var options = new ProvisioningClientOptions(new ProvisioningClientAmqpSettings());
            var transportHandler = new ProvisioningTransportHandlerAmqp(options);

            // act
            Action act = () => transportHandler.ValidateOutcome(rejected);

            // assert

            var error = act.Should().Throw<ProvisioningClientException>();
            error.And.ErrorCode.Should().Be(s_transientErrorDescription.ErrorCode);
            error.And.TrackingId.Should().Be(s_transientErrorDescription.TrackingId);
            error.And.Message.Should().Be(s_transientErrorDescription.Message);
            error.And.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public void ProvisioningTransportHandlerAmqp_ValidateOutcome_InvalidError()
        {
            // arrange

            var rejected = new Rejected() { Error = new Error() { Description = InvalidErrorDescription } };

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

        private class TestErrorDescription
        {
            public string Message { get; set; }

            public string TrackingId { get; set; }

            public int ErrorCode { get; set; }
        }
    }
}
