// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Runtime.Serialization;
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
        [DataRow(IotHubServiceErrorCode.DeviceNotFound)]
        [DataRow(IotHubServiceErrorCode.InvalidProtocolVersion)]
        [DataRow(IotHubServiceErrorCode.Unknown)]
        [DataRow(IotHubServiceErrorCode.InvalidOperation)]
        [DataRow(IotHubServiceErrorCode.ArgumentInvalid)]
        [DataRow(IotHubServiceErrorCode.ArgumentNull)]
        [DataRow(IotHubServiceErrorCode.IotHubFormatError)]
        [DataRow(IotHubServiceErrorCode.DeviceDefinedMultipleTimes)]
        [DataRow(IotHubServiceErrorCode.ModuleNotFound)]
        [DataRow(IotHubServiceErrorCode.BulkRegistryOperationFailure)]
        [DataRow(IotHubServiceErrorCode.IotHubSuspended)]
        [DataRow(IotHubServiceErrorCode.IotHubUnauthorizedAccess)]
        [DataRow(IotHubServiceErrorCode.DeviceMaximumQueueDepthExceeded)]
        [DataRow(IotHubServiceErrorCode.DeviceAlreadyExists)]
        [DataRow(IotHubServiceErrorCode.ModuleAlreadyExistsOnDevice)]
        [DataRow(IotHubServiceErrorCode.MessageTooLarge)]
        [DataRow(IotHubServiceErrorCode.TooManyDevices)]
        [DataRow(IotHubServiceErrorCode.PreconditionFailed)]
        public void IotHubServiceException_Ctor_WithNonTransientErrorCode_Ok(IotHubServiceErrorCode errorCode)
        {
            // arrange - act
            HttpStatusCode statusCode = HttpStatusCode.Accepted; // set for simplicity

            var exception = new IotHubServiceException(
                Message,
                statusCode,
                errorCode);

            // assert
            exception.StatusCode.Should().Be(statusCode);
            exception.Message.Should().Be(Message);
            exception.IsTransient.Should().BeFalse();
            exception.TrackingId.Should().BeNull();
            exception.ErrorCode.Should().Be(errorCode);
        }

        [TestMethod]
        [DataRow(IotHubServiceErrorCode.DeviceNotOnline)]
        [DataRow(IotHubServiceErrorCode.ServerError)]
        [DataRow(IotHubServiceErrorCode.IotHubQuotaExceeded)]
        [DataRow(IotHubServiceErrorCode.ServiceUnavailable)]
        [DataRow(IotHubServiceErrorCode.ThrottlingException)]
        public void IotHubServiceException_Ctor_WithTransientErrorCode_Ok(IotHubServiceErrorCode errorCode)
        {
            // arrange - act
            HttpStatusCode statusCode = HttpStatusCode.Accepted;  // set for simplicity

            var exception = new IotHubServiceException(
                Message,
                statusCode,
                errorCode);

            // assert
            exception.StatusCode.Should().Be(statusCode);
            exception.Message.Should().Be(Message);
            exception.IsTransient.Should().BeTrue();
            exception.TrackingId.Should().BeNull();
            exception.ErrorCode.Should().Be(errorCode);
        }
    }
}
