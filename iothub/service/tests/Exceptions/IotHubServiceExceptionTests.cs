// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests.Exceptions
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubServiceExceptionTests
    {
        private const string Message = "sample message";

        [TestMethod]
        public void IotHubServiceException_Ctor_WithNonTransientErrorCode_Ok()
        {
            // arrange - act
            HttpStatusCode statusCode = HttpStatusCode.NotFound;

            var exception = new IotHubServiceException(
                Message,
                statusCode,
                IotHubServiceErrorCode.DeviceNotFound);

            // assert
            exception.StatusCode.Should().Be(statusCode);
            exception.Message.Should().Be(Message);
            exception.IsTransient.Should().BeFalse();
            exception.TrackingId.Should().BeNull();
            exception.ErrorCode.Should().Be(IotHubServiceErrorCode.DeviceNotFound);

        }

        [TestMethod]
        public void IotHubServiceException_Ctor_WithTransidentErrorCode_Ok()
        {
            // arrange - act
            HttpStatusCode statusCode = HttpStatusCode.ServiceUnavailable;

            var exception = new IotHubServiceException(
                Message,
                statusCode,
                IotHubServiceErrorCode.ThrottlingException);

            // assert
            exception.StatusCode.Should().Be(statusCode);
            exception.Message.Should().Be(Message);
            exception.IsTransient.Should().BeTrue();
            exception.TrackingId.Should().BeNull();
        }

        [TestMethod]
        public void IotHubServiceException_UnknownErrorCode_IsNotTransient()
        {
            // arrange - act
            HttpStatusCode statusCode = HttpStatusCode.MovedPermanently; // not HttpStatusCode.RequestTimeout

            var exception = new IotHubServiceException(
                Message,
                statusCode,
                IotHubServiceErrorCode.Unknown);

            // assert
            exception.StatusCode.Should().Be(statusCode);
            exception.Message.Should().Be(Message);
            exception.IsTransient.Should().BeFalse();
            exception.TrackingId.Should().BeNull();
        }

        [TestMethod]
        public void IotHubServiceException_UnknownErrorCode_IsTransient()
        {
            // arrange - act
            HttpStatusCode statusCode = HttpStatusCode.RequestTimeout;

            var exception = new IotHubServiceException(
                Message,
                statusCode,
                IotHubServiceErrorCode.Unknown);

            // assert
            exception.StatusCode.Should().Be(statusCode);
            exception.Message.Should().Be(Message);
            exception.IsTransient.Should().BeTrue();
            exception.TrackingId.Should().BeNull();
        }

        [TestMethod]
        public void IotHubServiceException_GetObjectData_NullInfoThrows()
        {
            // arrange - act
            HttpStatusCode statusCode = HttpStatusCode.RequestTimeout;

            var exception = new IotHubServiceException(
                Message,
                statusCode,
                IotHubServiceErrorCode.Unknown);
            var sctx = new StreamingContext();

            // act
            Action act = () => exception.GetObjectData(null, sctx);

            // assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void IotHubServiceException_GetObjectData_Ok()
        {
            // arrange - act
            HttpStatusCode statusCode = HttpStatusCode.RequestTimeout;

            var exception = new IotHubServiceException(
                Message,
                statusCode,
                IotHubServiceErrorCode.Unknown);
            var sInfo = new SerializationInfo(GetType(), new FormatterConverter());
            var sctx = new StreamingContext();

            // act
            Action act = () => exception.GetObjectData(sInfo, sctx);

            // assert
            act.Should().NotThrow();
        }
    }
}
