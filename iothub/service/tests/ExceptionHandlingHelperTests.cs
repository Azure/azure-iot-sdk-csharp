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
        public async Task GetExceptionCodeAsync_ContentAndHeadersMatch_ValidErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                Message = "{\"errorCode\":404001}"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));
            httpResponseMessage.Headers.Add(HttpErrorCodeName, "DeviceNotFound");

            // act
            IotHubErrorCode errorCode = await ExceptionHandlingHelper.GetIotHubErrorCodeAsync(httpResponseMessage);

            // assert
            errorCode.Should().Be(IotHubErrorCode.DeviceNotFound);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_ContentAndHeadersMisMatch_InvalidErrorCode()
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
        public async Task GetExceptionCodeAsync_NoContentErrorCode_InvalidErrorCode()
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
        public async Task GetExceptionCodeAsync_NoHeaderErrorCodeName_InvalidErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                Message = "{\"errorCode\":404001}"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            IotHubErrorCode errorCode = await ExceptionHandlingHelper.GetIotHubErrorCodeAsync(httpResponseMessage);

            // assert
            errorCode.Should().Be(IotHubErrorCode.Unknown);
        }
    }
}
