﻿// Copyright (c) Microsoft. All rights reserved.
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
            // arrange
            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                Message = "{\"errorCode\":404001}"
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
            // arrange
            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                Message = "{\"errorCode\":404001}"
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
            // arrange
            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                Message = ""
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
            // arrange
            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            var exceptionResult = new IoTHubExceptionResult
            {
                Message = "{\"errorCode\":404001}"
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act
            ErrorCode errorCode = await ExceptionHandlingHelper.GetExceptionCodeAsync(httpResponseMessage);

            // assert
            Assert.AreEqual(ErrorCode.InvalidErrorCode, errorCode);
        }
    }
}
