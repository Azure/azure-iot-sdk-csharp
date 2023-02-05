// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            string timeoutExceptionMessage = "TimeoutException occurred";
            var timeoutException = new TimeoutException(timeoutExceptionMessage);

            var returnedException = (IotHubServiceException)AmqpClientHelper.ToIotHubClientContract(timeoutException);

            // assert
            returnedException.Message.Should().Be(timeoutExceptionMessage);
            returnedException.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
            returnedException.ErrorCode.Should().Be(IotHubServiceErrorCode.Unknown);
        }

        [TestMethod]
        public void AmqpClientHelper_ToIotHubClientContract_UnauthorizedAccessException_ReturnsToIHubServiceException()
        {
            // arrange - act
            string unauthorizedAccessExceptionMessage = "UnauthorizedAccessException occurred";
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
            string expectedTrackingId = "TrackingId1234";

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
        public void AmqpClientHelper_ToIotHubClientContract_NullErrorInAmqpException_ReturnsUnknownError()
        {
            // arrange - act
            var amqpException = new AmqpException(null);
            var returnedException = (IotHubServiceException)AmqpClientHelper.ToIotHubClientContract(amqpException);

            // assert
            returnedException.Message.Should().Be(UnknownErrorMessage);
        }

        [TestMethod]
        public void AmqpClientHelper_ToIotHubClientContract_NullError_NullInnerException_ReturnsUnknownError()
        {
            // arrange - act
            var returnedException = AmqpClientHelper.ToIotHubClientContract(null, null); // null error and innerException

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

        //[TestMethod]
        //public async Task AmqpClientHelper_GetObjectFromAmqpMessageAsync_FeedbackRecordType_Success()
        //{
        //    // arrange
        //    string payloadString = "Hello, World!";

        //    var message = new Message(payloadBytes);

        //    using AmqpMessage amqpMessage = MessageConverter.MessageToAmqpMessage(message);
        //    var dataList = new List<FeedbackRecord>
        //   {
        //        new FeedbackRecord
        //        {
        //            OriginalMessageId = "1",
        //            DeviceGenerationId = "d1",
        //            DeviceId = "deviceId1234",
        //            EnqueuedOnUtc= DateTime.UtcNow,
        //            StatusCode = FeedbackStatusCode.Success
        //        },
        //        new FeedbackRecord
        //        {
        //            OriginalMessageId = "2",
        //            DeviceGenerationId = "d2",
        //            DeviceId = "deviceId5678",
        //            EnqueuedOnUtc= DateTime.UtcNow,
        //            StatusCode = FeedbackStatusCode.Expired
        //        },
        //    };

        //    byte[] payloadBytes = Encoding.UTF8.GetBytes(dataList);

        //    using var amqpMessage = AmqpMessage.Create(new MemoryStream(dataList), true);
        //    var data = new Data
        //    {
        //        Value = (amqpMessage.ToStream()),
        //    };

        //    amqpMessage.Properties.ContentType = AmqpsConstants.BatchedFeedbackContentType;

        //    // act - assert
        //    IEnumerable<FeedbackRecord> feedbackRecords = await AmqpClientHelper.GetObjectFromAmqpMessageAsync<IEnumerable<FeedbackRecord>>(amqpMessage).ConfigureAwait(false);

        //    // assert
        //    feedbackRecords.Count().Should().Be(2);
        //}

        [TestMethod]
        public void AmqpClientHelper_GetErrorContextFromException_Success()
        {
            // arrange
            string amqpErrorCode = "amqp:not-found";
            string trackingId = "Trackingid1234";

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
