﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class DeviceClientTwinApiTests
    {
        private const string FakeHostName = "acme.azure-devices.net";
        private const string FakeDeviceId = "fake";
        private const string FakeSharedAccessKey = "dGVzdFN0cmluZzE=";
        private const string FakeSharedAccessKeyName = "AllAccessKey";
        private static readonly string s_fakeConnectionString = $"HostName={FakeHostName};SharedAccessKeyName={FakeSharedAccessKeyName};DeviceId={FakeDeviceId};SharedAccessKey={FakeSharedAccessKey}";

        [TestMethod]
        public async Task IotHubDeviceClient_SetDesiredPropertyUpdateCallbackAsyncRegistersForPatchesOnFirstCall()
        {
            // arrange
            var innerHandler = new Mock<IDelegatingHandler>();
            await using var client = new IotHubDeviceClient(s_fakeConnectionString);
            client.InnerHandler = innerHandler.Object;
            Func<DesiredProperties, Task> myCallback = (p) => Task.CompletedTask;

            // act
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.EnableTwinPatchAsync(It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_DesiredPropertyUpdateCallbackUnsubscribes()
        {
            // arrange
            var innerHandler = new Mock<IDelegatingHandler>();
            await using var client = new IotHubDeviceClient(s_fakeConnectionString);
            client.InnerHandler = innerHandler.Object;
            Func<DesiredProperties, Task> myCallback = (p) => Task.CompletedTask;

            // act
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback).ConfigureAwait(false);
            await client.SetDesiredPropertyUpdateCallbackAsync(null).ConfigureAwait(false);

            // assert
            innerHandler
                .Verify(x => x.DisableTwinPatchAsync(It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetDesiredPropertyUpdateCallbackAsyncDoesNotRegisterForPatchesAfterFirstCall()
        {
            // arrange
            var innerHandler = new Mock<IDelegatingHandler>();
            await using var client = new IotHubDeviceClient(s_fakeConnectionString);
            client.InnerHandler = innerHandler.Object;
            Func<DesiredProperties, Task> myCallback = (p) => Task.CompletedTask;

            // act
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback).ConfigureAwait(false);
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback).ConfigureAwait(false);
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.EnableTwinPatchAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_GetTwinAsyncCallsSendTwinGetAsync()
        {
            // arrange
            var innerHandler = new Mock<IDelegatingHandler>();
            await using var client = new IotHubDeviceClient(s_fakeConnectionString);
            client.InnerHandler = innerHandler.Object;

            // act
            await client.GetTwinPropertiesAsync().ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.GetTwinAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public void IotHubDeviceClient_Verify_GetTwinResponse()
        {
            // arrange
            var reported = new ReportedProperties()
            {
                PayloadConvention = DefaultPayloadConvention.Instance,
            };
            reported["$version"] = "1";
            reported.Add("key", "value");
            var desired = new DesiredProperties(new Dictionary<string, object>() { { "$version", "1" } });
            GetTwinResponse twinResponse = new GetTwinResponse
            {
                Status = 404,
                Twin = new TwinProperties(desired, reported),
                ErrorResponseMessage = new IotHubClientErrorResponseMessage
                {
                    ErrorCode = 404,
                    TrackingId = "Id",
                    Message = "message",
                    OccurredOnUtc = "00:00:00",
                },
            };

            // assert
            twinResponse.Status.Should().Be(404);
            twinResponse.Twin.Should().BeEquivalentTo(twinResponse.Twin);
            twinResponse.ErrorResponseMessage.Should().BeEquivalentTo(twinResponse.ErrorResponseMessage);
            twinResponse.ErrorResponseMessage.ErrorCode.Should().Be(twinResponse.ErrorResponseMessage?.ErrorCode);
            twinResponse.ErrorResponseMessage.TrackingId.Should().Be(twinResponse.ErrorResponseMessage?.TrackingId);
            twinResponse.ErrorResponseMessage.Message.Should().Be(twinResponse.ErrorResponseMessage?.Message);
            twinResponse.ErrorResponseMessage.OccurredOnUtc.Should().Be(twinResponse.ErrorResponseMessage?.OccurredOnUtc);
            twinResponse.Twin.Desired.Should().Equal(desired);
            twinResponse.Twin.Reported["$version"].Should().Be("1");
            twinResponse.Twin.Reported.GetObjectBytes().Should().NotBeNull();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_UpdateReportedPropertiesAsyncCallsSendTwinPatchAsync()
        {
            // arrange
            var innerHandler = new Mock<IDelegatingHandler>();
            await using var client = new IotHubDeviceClient(s_fakeConnectionString);
            client.InnerHandler = innerHandler.Object;
            var props = new ReportedProperties();

            // act
            await client.UpdateReportedPropertiesAsync(props).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.UpdateReportedPropertiesAsync(props, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_UpdateReportedPropertiesAsyncThrowsIfPatchIsNull()
        {
            // arrange
            var innerHandler = new Mock<IDelegatingHandler>();
            await using var client = new IotHubDeviceClient(s_fakeConnectionString);
            client.InnerHandler = innerHandler.Object;

            // act
            Func<Task> act = async () =>
            {
                await client.UpdateReportedPropertiesAsync(null).ConfigureAwait(false);
            };

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_CallbackAsyncIsCalledWhenPatchIsReceived()
        {
            // arrange
            var innerHandler = new Mock<IDelegatingHandler>();
            await using var client = new IotHubDeviceClient(s_fakeConnectionString);
            client.InnerHandler = innerHandler.Object;
            var myPatch = new DesiredProperties(new Dictionary<string, object> { { "key", "value" }, { "$version", 1 } })
            {
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            int callCount = 0;
            DesiredProperties receivedPatch = null;
            Task myCallback(DesiredProperties p)
            {
                callCount++;
                receivedPatch = p;
                return Task.CompletedTask;
            }
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback).ConfigureAwait(false);

            // act
            client.OnDesiredStatePatchReceived(myPatch);

            // assert
            callCount.Should().Be(1);
            myPatch.Should().BeEquivalentTo(receivedPatch);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_PatchIsReceived_NoCallback()
        {
            // arrange
            var innerHandler = new Mock<IDelegatingHandler>();
            await using var client = new IotHubDeviceClient(s_fakeConnectionString);
            client.InnerHandler = innerHandler.Object;
            var myPatch = new DesiredProperties(new Dictionary<string, object> { { "key", "value" }, { "$version", 1 } })
            {
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            int callCount = 0;
            await client.SetDesiredPropertyUpdateCallbackAsync(null).ConfigureAwait(false);

            // act
            client.OnDesiredStatePatchReceived(myPatch);

            //assert
            callCount.Should().Be(0);
        }
    }
}
