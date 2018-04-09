using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    public class DeviceClientTwinApiTests
    {
        static string fakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";

        // Tests_SRS_DEVICECLIENT_18_003: `SetDesiredPropertyUpdateCallback` shall call the transport to register for PATCHes on it's first call.
        [TestMethod]
        [TestCategory("Twin")]
        public async Task DeviceClientSetDesiredPropertyUpdateCallbackRegistersForPatchesOnFirstCall()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            DesiredPropertyUpdateCallback myCallback = (p, c) => { return TaskHelpers.CompletedTask; };
            var context = new object();

            // act
            await client.SetDesiredPropertyUpdateCallback(myCallback, context).ConfigureAwait(false);

            // assert
            await innerHandler.
                Received(1).
                EnableTwinPatchAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.AreEqual(client.desiredPropertyUpdateCallback, myCallback);
        }

        // Tests_SRS_DEVICECLIENT_18_003: `SetDesiredPropertyUpdateCallbackAsync` shall call the transport to register for PATCHes on it's first call.
        [TestMethod]
        [TestCategory("Twin")]
        public async Task DeviceClientSetDesiredPropertyUpdateCallbackAsyncRegistersForPatchesOnFirstCall()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            DesiredPropertyUpdateCallback myCallback = (p, c) => { return TaskHelpers.CompletedTask; };
            var context = new object();

            // act
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback, context).ConfigureAwait(false);

            // assert
            await innerHandler.
                Received(1).
                EnableTwinPatchAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.AreEqual(client.desiredPropertyUpdateCallback, myCallback);
        }

        // Tests_SRS_DEVICECLIENT_18_004: `SetDesiredPropertyUpdateCallback` shall not call the transport to register for PATCHes on subsequent calls
        [TestMethod]
        [TestCategory("Twin")]
        public async Task DeviceClientSetDesiredPropertyUpdateCallbackDoesNotRegisterForPatchesAfterFirstCall()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            DesiredPropertyUpdateCallback myCallback = (p, c) => { return TaskHelpers.CompletedTask; };

            // act
            await client.SetDesiredPropertyUpdateCallback(myCallback, null).ConfigureAwait(false);
            await client.SetDesiredPropertyUpdateCallback(myCallback, null).ConfigureAwait(false);
            await client.SetDesiredPropertyUpdateCallback(myCallback, null).ConfigureAwait(false);

            // assert
            await innerHandler.
                Received(1).
                EnableTwinPatchAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        // Tests_SRS_DEVICECLIENT_18_004: `SetDesiredPropertyUpdateCallbackAsync` shall not call the transport to register for PATCHes on subsequent calls
        [TestMethod]
        [TestCategory("Twin")]
        public async Task DeviceClientSetDesiredPropertyUpdateCallbackAsyncDoesNotRegisterForPatchesAfterFirstCall()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            DesiredPropertyUpdateCallback myCallback = (p, c) => { return TaskHelpers.CompletedTask; };

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
        [TestCategory("Twin")]
        public async Task DeviceClientGetTwinAsyncCallsSendTwinGetAsync()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
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
        [TestCategory("Twin")]
        public async Task DeviceClientUpdateReportedPropertiesAsyncCallsSendTwinPatchAsync()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
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
        [TestCategory("Twin")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DeviceClientUpdateReportedPropertiesAsyncThrowsIfPatchIsNull()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;

            // act and assert
            await client.UpdateReportedPropertiesAsync(null).ConfigureAwait(false);
        }

        // Tests_SRS_DEVICECLIENT_18_007: `SetDesiredPropertyUpdateCallback` shall throw an `ArgumentNull` exception if `callback` is null
        [TestMethod]
        [TestCategory("Twin")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DeviceClientSetDesiredPropertyUpdateCallbackThrowsIfCallbackIsNull()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;

            // act and assert
            await client.SetDesiredPropertyUpdateCallback(null, null).ConfigureAwait(false);
        }

        // Tests_SRS_DEVICECLIENT_18_007: `SetDesiredPropertyUpdateCallbackAsync` shall throw an `ArgumentNull` exception if `callback` is null
        [TestMethod]
        [TestCategory("Twin")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DeviceClientSetDesiredPropertyUpdateCallbackAsyncThrowsIfCallbackIsNull()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;

            // act and assert
            await client.SetDesiredPropertyUpdateCallbackAsync(null, null).ConfigureAwait(false);
        }

        //  Tests_SRS_DEVICECLIENT_18_005: When a patch is received from the service, the `callback` shall be called.
        [TestMethod]
        [TestCategory("Twin")]
        public async Task DeviceClientCallbackIsCalledWhenPatchIsReceived()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            var myPatch = new TwinCollection();

            int callCount = 0;
            TwinCollection receivedPatch = null;
            DesiredPropertyUpdateCallback myCallback = (p, c) => {
                callCount++;
                receivedPatch = p;
                return TaskHelpers.CompletedTask;
            };
            await client.SetDesiredPropertyUpdateCallback(myCallback, null).ConfigureAwait(false);

            // act
            client.OnReportedStatePatchReceived(myPatch);

            //assert
            Assert.AreEqual(callCount, 1);
            Assert.ReferenceEquals(myPatch, receivedPatch);
        }

        //  Tests_SRS_DEVICECLIENT_18_005: When a patch is received from the service, the `callback` shall be called.
        [TestMethod]
        [TestCategory("Twin")]
        public async Task DeviceClientCallbackAsyncIsCalledWhenPatchIsReceived()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            var myPatch = new TwinCollection();

            int callCount = 0;
            TwinCollection receivedPatch = null;
            DesiredPropertyUpdateCallback myCallback = (p, c) => {
                callCount++;
                receivedPatch = p;
                return TaskHelpers.CompletedTask;
            };
            await client.SetDesiredPropertyUpdateCallbackAsync(myCallback, null).ConfigureAwait(false);

            // act
            client.OnReportedStatePatchReceived(myPatch);

            //assert
            Assert.AreEqual(callCount, 1);
            Assert.ReferenceEquals(myPatch, receivedPatch);
        }
    }
}
