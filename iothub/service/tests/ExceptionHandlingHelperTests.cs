// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading.Tasks;
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
            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                Message = "{\"errorCode\":404001}"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));
            httpResponseMessage.Headers.Add(HttpErrorCodeName, "DeviceNotFound");

            // act
            IotHubStatusCode errorCode = await ExceptionHandlingHelper.GetExceptionCodeAsync(httpResponseMessage);

            // assert
            Assert.AreEqual(IotHubStatusCode.DeviceNotFound, errorCode);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_ContentAndHeadersMisMatch_InvalidErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                Message = "{\"errorCode\":404001}"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));
            httpResponseMessage.Headers.Add(HttpErrorCodeName, "DummyErrorCode");

            // act
            IotHubStatusCode errorCode = await ExceptionHandlingHelper.GetExceptionCodeAsync(httpResponseMessage);

            // assert
            Assert.AreEqual(IotHubStatusCode.Unknown, errorCode);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_NoContentErrorCode_InvalidErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                Message = ""
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));
            httpResponseMessage.Headers.Add(HttpErrorCodeName, "DeviceNotFound");

            // act
            IotHubStatusCode errorCode = await ExceptionHandlingHelper.GetExceptionCodeAsync(httpResponseMessage);

            // assert
            Assert.AreEqual(IotHubStatusCode.Unknown, errorCode);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_NoHeaderErrorCodeName_InvalidErrorCode()
        {
            // arrange
            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                Message = "{\"errorCode\":404001}"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            IotHubStatusCode errorCode = await ExceptionHandlingHelper.GetExceptionCodeAsync(httpResponseMessage);

            // assert
            Assert.AreEqual(IotHubStatusCode.Unknown, errorCode);
        }
    }
}
