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

        // Tests_SRS_DEVICECLIENT_18_003: `SetDesiredPropertyUpdateCallbackAsync` shall call the transport to register for PATCHes on it's first call.
        [TestMethod]
        public async Task DeviceClientSetDesiredPropertyUpdateCallbackAsyncRegistersForPatchesOnFirstCall()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = IotHubDeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            DesiredPropertyUpdateCallback myCallback = (p, c) => TaskHelpers.CompletedTask;
            var context = new object();

            // act
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback, context).ConfigureAwait(false);

            // assert
            await innerHandler.
                Received(1).
                EnableTwinPatchAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.AreEqual(client.InternalClient._desiredPropertyUpdateCallback, myCallback);
        }

        [TestMethod]
        public async Task DeviceClientDesiredPropertyUpdateCallbackUnsubscribes()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = IotHubDeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            DesiredPropertyUpdateCallback myCallback = (p, c) => TaskHelpers.CompletedTask;
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

        // Tests_SRS_DEVICECLIENT_18_004: `SetDesiredPropertyUpdateCallbackAsync` shall not call the transport to register for PATCHes on subsequent calls
        [TestMethod]
        public async Task DeviceClientSetDesiredPropertyUpdateCallbackAsyncDoesNotRegisterForPatchesAfterFirstCall()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = IotHubDeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            DesiredPropertyUpdateCallback myCallback = (p, c) => TaskHelpers.CompletedTask;

            // act
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback, null).ConfigureAwait(false);
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback, null).ConfigureAwait(false);
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback, null).ConfigureAwait(false);

            // assert
            await innerHandler.
                Received(1).
                EnableTwinPatchAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        // Tests_SRS_DEVICECLIENT_18_001: `GetTwinAsync` shall call `SendTwinGetAsync` on the transport to get the twin state
        [TestMethod]
        public async Task DeviceClientGetTwinAsyncCallsSendTwinGetAsync()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = IotHubDeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;

            // act
            await client.GetTwinAsync().ConfigureAwait(false);

            // assert
            await innerHandler.
                Received(1).
                SendTwinGetAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        // Tests_SRS_DEVICECLIENT_18_002: `UpdateReportedPropertiesAsync` shall call `SendTwinPatchAsync` on the transport to update the reported properties
        [TestMethod]
        public async Task DeviceClientUpdateReportedPropertiesAsyncCallsSendTwinPatchAsync()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = IotHubDeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            var props = new TwinCollection();

            // act
            await client.UpdateReportedPropertiesAsync(props).ConfigureAwait(false);

            // assert
            await innerHandler.
                Received(1).
                SendTwinPatchAsync(Arg.Is(props), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        // Tests_SRS_DEVICECLIENT_18_006: `UpdateReportedPropertiesAsync` shall throw an `ArgumentNull` exception if `reportedProperties` is null
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DeviceClientUpdateReportedPropertiesAsyncThrowsIfPatchIsNull()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = IotHubDeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;

            // act and assert
            await client.UpdateReportedPropertiesAsync(null).ConfigureAwait(false);
        }

        //  Tests_SRS_DEVICECLIENT_18_005: When a patch is received from the service, the `callback` shall be called.
        [TestMethod]
        public async Task DeviceClientCallbackAsyncIsCalledWhenPatchIsReceived()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = IotHubDeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            var myPatch = new TwinCollection();

            int callCount = 0;
            TwinCollection receivedPatch = null;
            DesiredPropertyUpdateCallback myCallback = (p, c) =>
            {
                callCount++;
                receivedPatch = p;
                return TaskHelpers.CompletedTask;
            };
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback, null).ConfigureAwait(false);

            // act
            client.InternalClient.OnReportedStatePatchReceived(myPatch);

            //assert
            Assert.AreEqual(callCount, 1);
            Assert.ReferenceEquals(myPatch, receivedPatch);
        }
    }
}
