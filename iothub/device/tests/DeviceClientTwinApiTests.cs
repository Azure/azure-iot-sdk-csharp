// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

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
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = new IotHubDeviceClient(fakeConnectionString);
            client.InnerHandler = innerHandler;
            Func<TwinCollection, object, Task> myCallback = (p, c) => Task.CompletedTask;
            var context = new object();

            // act
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback, context).ConfigureAwait(false);

            // assert
            await innerHandler.
                Received(1).
                EnableTwinPatchAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_DesiredPropertyUpdateCallbackUnsubscribes()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = new IotHubDeviceClient(fakeConnectionString);
            client.InnerHandler = innerHandler;
            Func<TwinCollection, object, Task> myCallback = (p, c) => Task.CompletedTask;
            var context = new object();

            // act
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback, context).ConfigureAwait(false);
            await client.SetDesiredPropertyUpdateCallbackAsync(null, null).ConfigureAwait(false);

            // assert
            await innerHandler
                .Received(1)
                .DisableTwinPatchAsync(Arg.Any<CancellationToken>())
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetDesiredPropertyUpdateCallbackAsyncDoesNotRegisterForPatchesAfterFirstCall()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = new IotHubDeviceClient(fakeConnectionString);
            client.InnerHandler = innerHandler;
            Func<TwinCollection, object, Task> myCallback = (p, c) => Task.CompletedTask;

            // act
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback, null).ConfigureAwait(false);
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback, null).ConfigureAwait(false);
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback, null).ConfigureAwait(false);

            // assert
            await innerHandler.
                Received(1).
                EnableTwinPatchAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_GetTwinAsyncCallsSendTwinGetAsync()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = new IotHubDeviceClient(fakeConnectionString);
            client.InnerHandler = innerHandler;

            // act
            await client.GetTwinAsync().ConfigureAwait(false);

            // assert
            await innerHandler.
                Received(1).
                SendTwinGetAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_UpdateReportedPropertiesAsyncCallsSendTwinPatchAsync()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = new IotHubDeviceClient(fakeConnectionString);
            client.InnerHandler = innerHandler;
            var props = new TwinCollection();

            // act
            await client.UpdateReportedPropertiesAsync(props).ConfigureAwait(false);

            // assert
            await innerHandler.
                Received(1).
                SendTwinPatchAsync(Arg.Is(props), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task IotHubDeviceClient_UpdateReportedPropertiesAsyncThrowsIfPatchIsNull()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = new IotHubDeviceClient(fakeConnectionString);
            client.InnerHandler = innerHandler;

            // act and assert
            await client.UpdateReportedPropertiesAsync(null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_CallbackAsyncIsCalledWhenPatchIsReceived()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = new IotHubDeviceClient(fakeConnectionString);
            client.InnerHandler = innerHandler;
            var myPatch = new TwinCollection();

            int callCount = 0;
            TwinCollection receivedPatch = null;
            Func<TwinCollection, object, Task> myCallback = (p, c) =>
            {
                callCount++;
                receivedPatch = p;
                return Task.CompletedTask;
            };
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback, null).ConfigureAwait(false);

            // act
            client.OnDesiredStatePatchReceived(myPatch);

            //assert
            Assert.AreEqual(callCount, 1);
            Assert.ReferenceEquals(myPatch, receivedPatch);
        }
    }
}
