// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests.Exceptions
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubServiceExceptionTests
    {

        [TestMethod]
        public void IotHubServiceException_ctor_not_transient_ok()
        {
            // arrange
            string message = "sample message";
            HttpStatusCode statusCode = HttpStatusCode.NotFound;

            var exception = new IotHubServiceException(
                message,
                statusCode,
                IotHubServiceErrorCode.DeviceNotFound);

            exception.StatusCode.Should().Be(statusCode);
            exception.Message.Should().Be(message);
            exception.IsTransient.Should().BeFalse();
            exception.TrackingId.Should().BeNull();
        }

        [TestMethod]
        public void IotHubServiceException_ctor_transient_ok()
        {
            // arrange
            string message = "sample message";
            HttpStatusCode statusCode = HttpStatusCode.ServiceUnavailable;

            var exception = new IotHubServiceException(
                message,
                statusCode,
                IotHubServiceErrorCode.ThrottlingException);

            exception.StatusCode.Should().Be(statusCode);
            exception.Message.Should().Be(message);
            exception.IsTransient.Should().BeTrue();
            exception.TrackingId.Should().BeNull();
        }

    }
}
