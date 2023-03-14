// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Tests.Amqp
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpClientHelperTests
    {
        private const string UnknownErrorMessage = "Unknown error.";
        private const string AmqpLinkReleaseMessage = "AMQP link released.";

        [TestMethod]
        public void AmqpClientHelper_ToIotHubClientContract_TimeoutException_ReturnsIoTHubServiceException()
        {
            // arrange - act
            const string timeoutExceptionMessage = "TimeoutException occurred";
            var timeoutException = new TimeoutException(timeoutExceptionMessage);

            var returnedException = (IotHubServiceException)AmqpClientHelper.ToIotHubClientContract(timeoutException);

            // assert
            returnedException.Message.Should().Be(timeoutExceptionMessage);
            returnedException.IsTransient.Should().BeTrue();
            returnedException.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
            returnedException.ErrorCode.Should().Be(IotHubServiceErrorCode.Unknown);
        }

        [TestMethod]
        public void AmqpClientHelper_ToIotHubClientContract_UnauthorizedAccessException_ReturnsToIHubServiceException()
        {
            // arrange - act
            const string unauthorizedAccessExceptionMessage = "UnauthorizedAccessException occurred";
            var unauthorizedAccessException = new UnauthorizedAccessException(unauthorizedAccessExceptionMessage);

            var returnedException = (IotHubServiceException)AmqpClientHelper.ToIotHubClientContract(unauthorizedAccessException);

            // assert
            returnedException.Message.Should().Be(unauthorizedAccessExceptionMessage);
            returnedException.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            returnedException.ErrorCode.Should().Be(IotHubServiceErrorCode.IotHubUnauthorizedAccess);
        }

        [TestMethod]
        [DataRow("amqp:not-found", HttpStatusCode.NotFound, IotHubServiceErrorCode.DeviceNotFound)]
        [DataRow(AmqpConstants.Vendor + ":timeout", HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown)]
        [DataRow("amqp:unauthorized-access", HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess)]
        [DataRow("amqp:link:message-size-exceeded", HttpStatusCode.RequestEntityTooLarge, IotHubServiceErrorCode.MessageTooLarge)]
        [DataRow("amqp:resource-limit-exceeded", HttpStatusCode.Forbidden, IotHubServiceErrorCode.DeviceMaximumQueueDepthExceeded)]
        [DataRow(AmqpConstants.Vendor + ":device-already-exists", HttpStatusCode.Conflict, IotHubServiceErrorCode.DeviceAlreadyExists)]
        [DataRow(AmqpConstants.Vendor + ":device-container-throttled", (HttpStatusCode)429, IotHubServiceErrorCode.ThrottlingException)]
        [DataRow(AmqpConstants.Vendor + ":quota-exceeded", HttpStatusCode.Forbidden, IotHubServiceErrorCode.IotHubQuotaExceeded)]
        [DataRow(AmqpConstants.Vendor + ":precondition-failed", HttpStatusCode.PreconditionFailed, IotHubServiceErrorCode.PreconditionFailed)]
        [DataRow(AmqpConstants.Vendor + ":iot-hub-suspended", HttpStatusCode.BadRequest, IotHubServiceErrorCode.IotHubSuspended)]
        public void AmqpClientHelper_ToIotHubClientContract_Success(string amqpErrorCode, HttpStatusCode statusCode, IotHubServiceErrorCode errorCode)
        {
            // arrange - act
            const string expectedTrackingId = "TrackingId1234";

            var error = new Error
            {
                Condition = amqpErrorCode,
                Info = new Fields
                {
                    { AmqpsConstants.TrackingId, expectedTrackingId }
                },
                Description = amqpErrorCode
            };
            string expectedMessage = $"{error.Description}\r\nTracking Id:{expectedTrackingId}";

            var amqpException = new AmqpException(error);
            var returnedException = (IotHubServiceException)AmqpClientHelper.ToIotHubClientContract(amqpException);

            // assert
            returnedException.StatusCode.Should().Be(statusCode);
            returnedException.Message.Should().Be(expectedMessage);
            returnedException.ErrorCode.Should().Be(errorCode);
            returnedException.TrackingId.Should().Be(expectedTrackingId);
        }

        [TestMethod]
        public void AmqpClientHelper_ToIotHubClientContract_NullError_NullInnerException_ReturnsUnknownError()
        {
            // arrange - act
            IotHubServiceException returnedException = AmqpClientHelper.ToIotHubClientContract(null, null); // null error and innerException

            // assert
            returnedException.Message.Should().Be(UnknownErrorMessage);
        }

        [TestMethod]
        public void AmqpClientHelper_ValidateContentType_OnValidationFailure_ThrowsInvalidOperationException()
        {
            // arrange
            using var amqpMessage = AmqpMessage.Create();
            amqpMessage.Properties.ContentType = "application/json";

            // act
            Action act = () => AmqpClientHelper.ValidateContentType(amqpMessage, AmqpsConstants.BatchedFeedbackContentType);
           
            // assert
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void AmqpClientHelper_ValidateContentType_Success()
        {
            // arrange
            using var amqpMessage = AmqpMessage.Create();
            amqpMessage.Properties.ContentType = AmqpsConstants.BatchedFeedbackContentType;

            // act
            Action act = () => AmqpClientHelper.ValidateContentType(amqpMessage, AmqpsConstants.BatchedFeedbackContentType);

            // assert
            act.Should().NotThrow<Exception>();
        }

        [TestMethod]
        public void AmqpClientHelper_GetExceptionFromOutcome_NullOutcome_ReturnsIotHubServiceException_WithUnknownMessage()
        {
            // arrange - act
            var act = (IotHubServiceException)AmqpClientHelper.GetExceptionFromOutcome(null);

            // assert
            act.Message.Should().Be(UnknownErrorMessage);
        }

        [TestMethod]
        public void AmqpClientHelper_GetExceptionFromOutcome_RejectedOutcome_ReturnsIotHubServiceException_WithRejectedMessage()
        {
            // arrange
            var outcome = new Rejected();

            // act
            var act = (IotHubServiceException)AmqpClientHelper.GetExceptionFromOutcome(outcome);

            // assert
            act.Message.Should().Be(UnknownErrorMessage);
        }

        [TestMethod]
        public void AmqpClientHelper_GetExceptionFromOutcome_ReleasedOutcome_ReturnsOperationCanceledException()
        {
            // arrange
            var outcome = new Released();

            // act
            var act = (OperationCanceledException)AmqpClientHelper.GetExceptionFromOutcome(outcome);

            // assert
            act.Message.Should().Be(AmqpLinkReleaseMessage);
        }


        [TestMethod]
        public void AmqpClientHelper_GetExceptionFromOutcome_NonRejectedOutcome_NonReleased_ReturnsIotHubServiceException_WithUnknownMessage()
        {
            // arrange
            var outcome = new Accepted();

            // act
            var act = (IotHubServiceException)AmqpClientHelper.GetExceptionFromOutcome(outcome);

            // assert
            act.Message.Should().Be(UnknownErrorMessage);
        }

        [TestMethod]
        public async Task AmqpClientHelper_GetObjectFromAmqpMessageAsync_FeedbackRecordType_Success()
        {
            // arrange
            const string originalMessageId1 = "1";
            const string originalMessageId2 = "2";
            const string deviceGenerationId1 = "d1";
            const string deviceGenerationId2 = "d2";
            const string deviceId1 = "deviceId1234";
            const string deviceId2 = "deviceId5678";
            DateTime enqueuedOnUtc1 = DateTime.UtcNow;
            DateTime enqueuedOnUtc2 = DateTime.UtcNow.AddMinutes(2);
            FeedbackStatusCode statusCode1 = FeedbackStatusCode.Success;
            FeedbackStatusCode statusCode2 = FeedbackStatusCode.Expired;

            var dataList = new List<FeedbackRecord>
            {
                new FeedbackRecord
                {
                    OriginalMessageId = originalMessageId1,
                    DeviceGenerationId = deviceGenerationId1,
                    DeviceId = deviceId1,
                    EnqueuedOnUtc = enqueuedOnUtc1,
                    StatusCode = statusCode1
                },
                new FeedbackRecord
                {
                    OriginalMessageId = originalMessageId2,
                    DeviceGenerationId = deviceGenerationId2,
                    DeviceId = deviceId2,
                    EnqueuedOnUtc = enqueuedOnUtc2,
                    StatusCode = statusCode2
                },
            };

            var message = new OutgoingMessage(dataList);

            using AmqpMessage amqpMessage = MessageConverter.MessageToAmqpMessage(message);
            amqpMessage.Properties.ContentType = AmqpsConstants.BatchedFeedbackContentType;

            // act
            IEnumerable<FeedbackRecord> feedbackRecords = await AmqpClientHelper.GetObjectFromAmqpMessageAsync<IEnumerable<FeedbackRecord>>(amqpMessage).ConfigureAwait(false);

            // assert
            feedbackRecords.Count().Should().Be(dataList.Count);
            FeedbackRecord feedbackRecord1 = feedbackRecords.ElementAt(0);
            FeedbackRecord feedbackRecord2 = feedbackRecords.ElementAt(1);

            feedbackRecord1.OriginalMessageId.Should().Be(originalMessageId1);
            feedbackRecord2.OriginalMessageId.Should().Be(originalMessageId2);
            feedbackRecord1.DeviceGenerationId.Should().Be(deviceGenerationId1);
            feedbackRecord2.DeviceGenerationId.Should().Be(deviceGenerationId2);
            feedbackRecord1.DeviceId.Should().Be(deviceId1);
            feedbackRecord2.DeviceId.Should().Be(deviceId2);
            feedbackRecord1.EnqueuedOnUtc.Should().Be(enqueuedOnUtc1);
            feedbackRecord2.EnqueuedOnUtc.Should().Be(enqueuedOnUtc2);
            feedbackRecord1.StatusCode.Should().Be(statusCode1);
            feedbackRecord2.StatusCode.Should().Be(statusCode2);
        }

        [TestMethod]
        public void AmqpClientHelper_GetErrorContextFromException_Success()
        {
            // arrange
            const string amqpErrorCode = "amqp:not-found";
            const string trackingId = "Trackingid1234";

            var error = new Error
            {
                Condition = amqpErrorCode,
                Info = new Fields
                {
                    { AmqpsConstants.TrackingId, trackingId }
                },
                Description = amqpErrorCode
            };
            var amqpException = new AmqpException(error);

            // act
            ErrorContext act = AmqpClientHelper.GetErrorContextFromException(amqpException);

            // assert
            act.IotHubServiceException.Message.Should().Be(error.ToString());
            act.IotHubServiceException.InnerException.Should().BeEquivalentTo(amqpException);
        }
    }
}
