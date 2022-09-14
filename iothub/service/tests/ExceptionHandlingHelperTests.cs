// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ExceptionHandlingHelperTests
    {
        private const string HttpErrorCodeName = "iothub-errorcode";

        [TestMethod]
        public async Task GetExceptionCodeAsync_NumericErrorCode_InResponseMessage_ValidErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var responseMessage = new ResponseMessage
            {
                ErrorCode = "404103",
                TrackingId = "b575211ff5194d56b18721941e82c3d5",
                Message = "The operation failed because the requested device isn't online or hasn't registered the direct method callback.",
                OccurredOnUtc = "2022-09-12T21:59:47.99936Z",
            };
            var exceptionResult = new IoTHubExceptionResult
            {
                // A read-world message in the response content which includes the numeric error code returned by the hub service.
                Message = JsonConvert.SerializeObject(responseMessage),
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            Tuple<string, IotHubErrorCode> pair = await ExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage);
            string trackingId = pair.Item1;
            IotHubErrorCode errorCode = pair.Item2;

            // assert
            trackingId.Should().Be("b575211ff5194d56b18721941e82c3d5");
            errorCode.Should().Be(IotHubErrorCode.DeviceNotOnline);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_NonNumericErrorCode_InPlainString_ValidErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                // A read-world message in the response content which includes the non-numeric error code returned by the hub service.
                Message = "ErrorCode:PreconditionFailed;Precondition failed: Device version did not match, existingVersion:957482805 newVersion:957482804"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            Tuple<string, IotHubErrorCode> pair = await ExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage);
            IotHubErrorCode errorCode = pair.Item2;

            // assert
            errorCode.Should().Be(IotHubErrorCode.PreconditionFailed);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_InvalidContent_InJsonString_UnknownErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                // Invalid field name of error code in the response content.
                Message =
                "{" +
                    "\"InvalidField\":404103," +
                    "\"trackingId\":\"b575211ff5194d56b18721941e82c3d5\"," +
                    "\"message\":\"The operation failed because the requested device isn't online or hasn't registered the direct method callback.\"," +
                    "\"info\":{}," +
                    "\"timestampUtc\":\"2022-09-12T21:59:47.99936Z\"" +
                "}"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            Tuple<string, IotHubErrorCode> pair = await ExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage);
            IotHubErrorCode errorCode = pair.Item2;

            // assert
            errorCode.Should().Be(IotHubErrorCode.Unknown);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_InvalidContent_InPlainString_UnknownErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                // Invalid field name of error code in the response content.
                Message = "InvalidField:PreconditionFailed;Precondition failed: Device version did not match, existingVersion:957482805 newVersion:957482804"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            Tuple<string, IotHubErrorCode> pair = await ExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage);
            IotHubErrorCode errorCode = pair.Item2;

            // assert
            errorCode.Should().Be(IotHubErrorCode.Unknown);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_NoContentErrorCode_UnknownErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                Message = ""
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            Tuple<string, IotHubErrorCode> pair = await ExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage);
            IotHubErrorCode errorCode = pair.Item2;

            // assert
            errorCode.Should().Be(IotHubErrorCode.Unknown);
        }
    }
}
