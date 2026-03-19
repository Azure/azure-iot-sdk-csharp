// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using FluentAssertions;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ClientExceptionHandlingHelperTests
    {
        [TestMethod]
        public async Task GetErrorCodeAndTrackingIdAsync_MessagePayloadDoubleEscaped_ValidErrorCodeAndTrackingId()
        {
            // arrange

            const IotHubClientErrorCode expectedErrorCode = IotHubClientErrorCode.IotHubFormatError;
            const string expectedTrackingId = "E8A1D62DF1FB4F2F908B2F1492620D6B-G2";

            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IotHubExceptionResult
            {
                Message = JsonConvert.SerializeObject(
                    new ErrorPayload
                    {
                        ErrorCode = ((int)expectedErrorCode).ToString(),
                        TrackingId = expectedTrackingId,
                        Message = "Cannot decode correlation_id",
                        OccurredOnUtc = "2023-03-14T16:57:54.324613222+00:00",
                    }),
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act

            Tuple<string, IotHubClientErrorCode> pair = await ClientExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage).ConfigureAwait(false);
            string trackingId = pair.Item1;
            IotHubClientErrorCode errorCode = pair.Item2;

            // assert

            trackingId.Should().Be(expectedTrackingId);
            errorCode.Should().Be(expectedErrorCode);
        }

        [TestMethod]
        public async Task GetErrorCodeAndTrackingIdAsync_ErrorCodeIsNumeric_ValidErrorCodeAndTrackingId()
        {
            // arrange

            const IotHubClientErrorCode expectedErrorCode = IotHubClientErrorCode.IotHubFormatError;
            const string expectedTrackingId = "E8A1D62DF1FB4F2F908B2F1492620D6B-G2";

            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IotHubExceptionResult
            {
                Message = JsonConvert.SerializeObject(
                    new ErrorPayload
                    {
                        ErrorCode = (int)expectedErrorCode,
                        TrackingId = expectedTrackingId,
                        Message = "Cannot decode correlation_id",
                        OccurredOnUtc = "2023-03-14T16:57:54.324613222+00:00",
                    }),
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act

            Tuple<string, IotHubClientErrorCode> pair = await ClientExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage).ConfigureAwait(false);
            string trackingId = pair.Item1;
            IotHubClientErrorCode errorCode = pair.Item2;

            // assert

            trackingId.Should().Be(expectedTrackingId);
            errorCode.Should().Be(expectedErrorCode);
        }

        [TestMethod]
        public async Task GetErrorCodeAndTrackingIdAsync_NoContentErrorCode_UnknownErrorCodeAndEmptyTrackingId()
        {
            // arrange

            using var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var exceptionResult = new IotHubExceptionResult
            {
                Message = "",
            };
            httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(exceptionResult));

            // act

            Tuple<string, IotHubClientErrorCode> pair = await ClientExceptionHandlingHelper.GetErrorCodeAndTrackingIdAsync(httpResponseMessage).ConfigureAwait(false);
            string trackingId = pair.Item1;
            IotHubClientErrorCode errorCode = pair.Item2;

            // assert

            trackingId.Should().BeEmpty();
            errorCode.Should().Be(IotHubClientErrorCode.Unknown);
        }
    }
}
