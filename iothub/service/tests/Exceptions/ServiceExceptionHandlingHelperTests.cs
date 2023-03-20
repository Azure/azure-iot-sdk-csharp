// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Tests.Exceptions
{
    [TestClass]
    [TestCategory("Unit")]
    public class ServiceExceptionHandlingHelperTests

    {
        [TestMethod]
        public async Task GetErrorCodeAndTrackingIdAsync_NumericErrorCode_InResponseMessage_ValidErrorCode()
        {
            // arrange
            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IotHubExceptionResult
            {
                // A read-world message in the response content which includes the numeric error code returned by the hub service.
                Message = new ErrorPayload1
                {
                    ErrorCode = "404103",
                    TrackingId = "b575211ff5194d56b18721941e82c3d5",
                    Message = "The operation failed because the requested device isn't online or hasn't registered the direct method callback.",
                    OccurredOnUtc = "2022-09-12T21:59:47.99936Z",
                },
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            Tuple<string, IotHubServiceErrorCode> pair = await ServiceExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage).ConfigureAwait(false);
            string trackingId = pair.Item1;
            IotHubServiceErrorCode errorCode = pair.Item2;

            // assert
            trackingId.Should().Be("b575211ff5194d56b18721941e82c3d5");
            errorCode.Should().Be(IotHubServiceErrorCode.DeviceNotOnline);
        }

        [TestMethod]
        public async Task GetErrorCodeAndTrackingIdAsync_MessagePayloadDoubleEscaped()
        {
            // arrange
            const string expectedTrackingId = "95ae23a6a159445681f6a52aebc99ab0-TimeStamp:10/19/2022 16:47:22";
            const IotHubServiceErrorCode expectedErrorCode = IotHubServiceErrorCode.DeviceNotOnline;

            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new ResponseMessageWrapper
            {
                Message = JsonConvert.SerializeObject(new ErrorPayload1
                {
                    ErrorCode = ((int)expectedErrorCode).ToString(),
                    TrackingId = expectedTrackingId,
                    Message = "The operation failed because the requested device isn't online or hasn't registered the direct method callback. To learn more, see https://aka.ms/iothub404103",
                    OccurredOnUtc = "2022-10-19T16:47:22.0203257Z",
                })
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            Tuple<string, IotHubServiceErrorCode> pair = await ServiceExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage).ConfigureAwait(false);
            string trackingId = pair.Item1;
            IotHubServiceErrorCode errorCode = pair.Item2;

            // assert
            trackingId.Should().Be(expectedTrackingId);
            errorCode.Should().Be(expectedErrorCode);
        }

        [TestMethod]
        public async Task GetErrorCodeAndTrackingIdAsync_StructuredBodyFormat2()
        {
            // arrange
            const string expectedTrackingId = "aeec4c1e4e914a4c9f40fdba7be68fa5-G:0-TimeStamp:10/18/2022 20:50:39";
            const IotHubServiceErrorCode expectedErrorCode = IotHubServiceErrorCode.DeviceNotFound;
            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            string exceptionResult = $"{{\"Message\": \"ErrorCode:{expectedErrorCode};E2E_MessageReceiveE2EPoolAmqpTests__3_Sasl_f16d18b2-97dc-4ea5-86f1-a3405ee98939\",\"ExceptionMessage\":\"Tracking ID:{expectedTrackingId}\"}}";
            httpResponseMessage.Content = new StringContent(exceptionResult);

            // act
            Tuple<string, IotHubServiceErrorCode> result = await ServiceExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage).ConfigureAwait(false);

            // assert
            result.Item1.Should().Be(expectedTrackingId);
            result.Item2.Should().Be(expectedErrorCode);
        }

        [TestMethod]
        public async Task GetErrorCodeAndTrackingIdAsync_NonNumericErrorCode_InPlainString_ValidErrorCode()
        {
            // arrange
            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IotHubExceptionResult2
            {
                // A read-world message in the response content which includes the non-numeric error code returned by the hub service.
                Message = "ErrorCode:PreconditionFailed;Precondition failed: Device version did not match, existingVersion:957482805 newVersion:957482804"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            Tuple<string, IotHubServiceErrorCode> pair = await ServiceExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage).ConfigureAwait(false);
            IotHubServiceErrorCode errorCode = pair.Item2;

            // assert
            errorCode.Should().Be(IotHubServiceErrorCode.PreconditionFailed);
        }

        [TestMethod]
        public async Task GetErrorCodeAndTrackingIdAsync_InvalidContent_InPlainString_UnknownErrorCode()
        {
            // arrange
            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IotHubExceptionResult2
            {
                // Invalid field name of error code in the response content.
                Message = "InvalidField:PreconditionFailed;Precondition failed: Device version did not match, existingVersion:957482805 newVersion:957482804"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            Tuple<string, IotHubServiceErrorCode> pair = await ServiceExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage).ConfigureAwait(false);
            IotHubServiceErrorCode errorCode = pair.Item2;

            // assert
            errorCode.Should().Be(IotHubServiceErrorCode.Unknown);
        }

        [TestMethod]
        public async Task GetErrorCodeAndTrackingIdAsync_NoContentErrorCode_UnknownErrorCode()
        {
            // arrange
            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IotHubExceptionResult2
            {
                Message = ""
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            Tuple<string, IotHubServiceErrorCode> pair = await ServiceExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage).ConfigureAwait(false);
            IotHubServiceErrorCode errorCode = pair.Item2;

            // assert
            errorCode.Should().Be(IotHubServiceErrorCode.Unknown);
        }
    }
}
