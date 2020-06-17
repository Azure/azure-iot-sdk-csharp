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
        [TestMethod]
        public async Task GetExceptionCodeAsync_ContentAndHeadersMatch_ValidErrorCode()
        {
            // setup
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            IoTHubExceptionResult exceptionResult = new IoTHubExceptionResult
            {
                _message = "{\"errorCode\":404001}"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));
            httpResponseMessage.Headers.Add(CommonConstants.HttpErrorCodeName, "DeviceNotFound");

            // act
            ErrorCode errorCode = await ExceptionHandlingHelper.GetExceptionCodeAsync(httpResponseMessage);

            // assert
            Assert.AreEqual(ErrorCode.DeviceNotFound, errorCode);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_ContentAndHeadersMisMatch_InvalidErrorCode()
        {
            // setup
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            IoTHubExceptionResult exceptionResult = new IoTHubExceptionResult
            {
                _message = "{\"errorCode\":404001}"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));
            httpResponseMessage.Headers.Add(CommonConstants.HttpErrorCodeName, "DummyErrorCode");

            // act
            ErrorCode errorCode = await ExceptionHandlingHelper.GetExceptionCodeAsync(httpResponseMessage);

            // assert
            Assert.AreEqual(ErrorCode.InvalidErrorCode, errorCode);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_NoContentErrorCode_InvalidErrorCode()
        {
            // setup
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            IoTHubExceptionResult exceptionResult = new IoTHubExceptionResult
            {
                _message = ""
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));
            httpResponseMessage.Headers.Add(CommonConstants.HttpErrorCodeName, "DeviceNotFound");

            // act
            ErrorCode errorCode = await ExceptionHandlingHelper.GetExceptionCodeAsync(httpResponseMessage);

            // assert
            Assert.AreEqual(ErrorCode.InvalidErrorCode, errorCode);
        }

        [TestMethod]
        public async Task GetExceptionCodeAsync_NoHeaderErrorCodeName_InvalidErrorCode()
        {
            // setup
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            IoTHubExceptionResult exceptionResult = new IoTHubExceptionResult
            {
                _message = "{\"errorCode\":404001}"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            ErrorCode errorCode = await ExceptionHandlingHelper.GetExceptionCodeAsync(httpResponseMessage);

            // assert
            Assert.AreEqual(ErrorCode.InvalidErrorCode, errorCode);
        }
    }
}
