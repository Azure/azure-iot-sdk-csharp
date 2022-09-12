// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class ExceptionHandlingHelperTests
    {
        private const string HttpErrorCodeName = "iothub-errorcode";

        [TestMethod]
        public async Task GetExceptionCodeAsync_ContentAndHeadersMatch_NumericErrorCode_ValidErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                // A read-world message in the response content which includes the numeric error code returned by the hub service.
                Message = 
                "{" +
                    "\"errorCode\":404103," +
                    "\"trackingId\":\"b575211ff5194d56b18721941e82c3d5\"," +
                    "\"message\":\"The operation failed because the requested device isn't online or hasn't registered the direct method callback.\"," +
                    "\"info\":{}," +
                    "\"timestampUtc\":\"2022-09-12T21:59:47.99936Z\"" +
                "}"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));
            httpResponseMessage.Headers.Add(HttpErrorCodeName, "DeviceNotOnline");

            // act
            IotHubErrorCode errorCode = await ExceptionHandlingHelper.GetIotHubErrorCodeAsync(httpResponseMessage);

            // assert
            errorCode.Should().Be(IotHubErrorCode.DeviceNotOnline);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_ContentAndHeadersMatch_NonNumericErrorCode_ValidErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                // A read-world message in the response content which includes the non-numeric error code returned by the hub service.
                Message = "ErrorCode:PreconditionFailed;Precondition failed: Device version did not match, existingVersion:957482805 newVersion:957482804"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));
            httpResponseMessage.Headers.Add(HttpErrorCodeName, "PreconditionFailed");

            // act
            IotHubErrorCode errorCode = await ExceptionHandlingHelper.GetIotHubErrorCodeAsync(httpResponseMessage);

            // assert
            errorCode.Should().Be(IotHubErrorCode.PreconditionFailed);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_ContentAndHeadersMismatch_UnknownErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                Message = "{\"errorCode\":404001}"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));
            httpResponseMessage.Headers.Add(HttpErrorCodeName, "DummyErrorCode");

            // act
            IotHubErrorCode errorCode = await ExceptionHandlingHelper.GetIotHubErrorCodeAsync(httpResponseMessage);

            // assert
            errorCode.Should().Be(IotHubErrorCode.Unknown);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_InvalidContent_UnknownErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                // Invalid field name of error code in the response content.
                Message = "InvalidField:PreconditionFailed;Precondition failed: Device version did not match, existingVersion:957482805 newVersion:957482804"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));
            httpResponseMessage.Headers.Add(HttpErrorCodeName, "PreconditionFailed");

            // act
            IotHubErrorCode errorCode = await ExceptionHandlingHelper.GetIotHubErrorCodeAsync(httpResponseMessage);

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
            httpResponseMessage.Headers.Add(HttpErrorCodeName, "DeviceNotFound");

            // act
            IotHubErrorCode errorCode = await ExceptionHandlingHelper.GetIotHubErrorCodeAsync(httpResponseMessage);

            // assert
            errorCode.Should().Be(IotHubErrorCode.Unknown);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_NoHeaderErrorCode_UnknownErrorCode()
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
            IotHubErrorCode errorCode = await ExceptionHandlingHelper.GetIotHubErrorCodeAsync(httpResponseMessage);

            // assert
            errorCode.Should().Be(IotHubErrorCode.Unknown);
        }
    }
}
