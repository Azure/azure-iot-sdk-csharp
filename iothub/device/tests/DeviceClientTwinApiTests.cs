// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class DeviceClientTwinApiTests
    {
        private static string fakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";

        [TestMethod]
        public async Task IotHubDeviceClient_SetDesiredPropertyUpdateCallbackAsyncRegistersForPatchesOnFirstCall()
        {
            // arrange
            var innerHandler = new Mock<IDelegatingHandler>();
            var client = new IotHubDeviceClient(fakeConnectionString);
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
            var client = new IotHubDeviceClient(fakeConnectionString);
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
            var client = new IotHubDeviceClient(fakeConnectionString);
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
            var client = new IotHubDeviceClient(fakeConnectionString);
            client.InnerHandler = innerHandler.Object;

            // act
            await client.GetTwinPropertiesAsync().ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.GetTwinAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_UpdateReportedPropertiesAsyncCallsSendTwinPatchAsync()
        {
            // arrange
            var innerHandler = new Mock<IDelegatingHandler>();
            var client = new IotHubDeviceClient(fakeConnectionString);
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
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task IotHubDeviceClient_UpdateReportedPropertiesAsyncThrowsIfPatchIsNull()
        {
            // arrange
            var innerHandler = new Mock<IDelegatingHandler>();
            var client = new IotHubDeviceClient(fakeConnectionString);
            client.InnerHandler = innerHandler.Object;

            // act and assert
            await client.UpdateReportedPropertiesAsync(null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_CallbackAsyncIsCalledWhenPatchIsReceived()
        {
            // arrange
            var innerHandler = new Mock<IDelegatingHandler>();
            var client = new IotHubDeviceClient(fakeConnectionString);
            client.InnerHandler = innerHandler.Object;
            var myPatch = new DesiredProperties(new Dictionary<string, object> { { "key", "value" }, { "$version", 1 } })
            {
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            int callCount = 0;
            DesiredProperties receivedPatch = null;
            Func<DesiredProperties, Task> myCallback = (p) =>
            {
                callCount++;
                receivedPatch = p;
                return Task.CompletedTask;
            };
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback).ConfigureAwait(false);

            // act
            client.OnDesiredStatePatchReceived(myPatch);

            //assert
            Assert.AreEqual(callCount, 1);
            Assert.ReferenceEquals(myPatch, receivedPatch);
        }
    }
}
