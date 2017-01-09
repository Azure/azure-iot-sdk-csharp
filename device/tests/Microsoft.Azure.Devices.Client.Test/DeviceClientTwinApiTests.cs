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
        public async Task DeviceClient_SetDesiredPropertyUpdateCallback_RegistersForPatchesOnFirstCall()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            DesiredPropertyUpdateCallback myCallback = (p, c) => { return TaskHelpers.CompletedTask; };
            var context = new object();

            // act
            await client.SetDesiredPropertyUpdateCallback(myCallback, context);

            // assert
            await innerHandler.
                Received(1).
                EnableTwinPatchAsync(Arg.Any<CancellationToken>());
            Assert.AreEqual(client.desiredPropertyUpdateCallback, myCallback);
        }

        // Tests_SRS_DEVICECLIENT_18_004: `SetDesiredPropertyUpdateCallback` shall not call the transport to register for PATCHes on subsequent calls
        [TestMethod]
        [TestCategory("Twin")]
        public async Task DeviceClient_SetDesiredPropertyUpdateCallback_DoesNotRegisterForPatchesAfterFirstCall()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            DesiredPropertyUpdateCallback myCallback = (p, c) => { return TaskHelpers.CompletedTask; };

            // act
            await client.SetDesiredPropertyUpdateCallback(myCallback, null);
            await client.SetDesiredPropertyUpdateCallback(myCallback, null);
            await client.SetDesiredPropertyUpdateCallback(myCallback, null);

            // assert
            await innerHandler.
                Received(1).
                EnableTwinPatchAsync(Arg.Any<CancellationToken>());
        }

        // Tests_SRS_DEVICECLIENT_18_001: `GetTwinAsync` shall call `SendTwinGetAsync` on the transport to get the twin state
        [TestMethod]
        [TestCategory("Twin")]
        public async Task DeviceClient_GetTwinAsync_CallsSendTwinGetAsync()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;

            // act
            await client.GetTwinAsync();

            // assert
            await innerHandler.
                Received(1).
                SendTwinGetAsync(Arg.Any<CancellationToken>());
        }

        // Tests_SRS_DEVICECLIENT_18_002: `UpdateReportedPropertiesAsync` shall call `SendTwinPatchAsync` on the transport to update the reported properties
        [TestMethod]
        [TestCategory("Twin")]
        public async Task DeviceClient_UpdateReportedPropertiesAsync_CallsSendTwinPatchAsync()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;
            var props = new TwinCollection();

            // act
            await client.UpdateReportedPropertiesAsync(props);

            // assert
            await innerHandler.
                Received(1).
                SendTwinPatchAsync(Arg.Is<TwinCollection>(props), Arg.Any<CancellationToken>());
        }

        // Tests_SRS_DEVICECLIENT_18_006: `UpdateReportedPropertiesAsync` shall throw an `ArgumentNull` exception if `reportedProperties` is null
        [TestMethod]
        [TestCategory("Twin")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DeviceClient_UpdateReportedPropertiesAsync_ThrowsIfPatchIsNull()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;

            // act and assert
            await client.UpdateReportedPropertiesAsync(null);
        }

        // Tests_SRS_DEVICECLIENT_18_007: `SetDesiredPropertyUpdateCallback` shall throw an `ArgumentNull` exception if `callback` is null
        [TestMethod]
        [TestCategory("Twin")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DeviceClient_SetDesiredPropertyUpdateCallback_ThrowsIfCallbackIsNull()
        {
            // arrange
            var innerHandler = Substitute.For<IDelegatingHandler>();
            var client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.InnerHandler = innerHandler;

            // act and assert
            await client.SetDesiredPropertyUpdateCallback(null, null);
        }

        //  Tests_SRS_DEVICECLIENT_18_005: When a patch is received from the service, the `callback` shall be called.
        [TestMethod]
        [TestCategory("Twin")]
        public async Task DeviceClient_callbackIsCalledWhenPatchIsReceived()
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
            await client.SetDesiredPropertyUpdateCallback(myCallback, null);

            // act
            innerHandler.TwinUpdateHandler(myPatch);

            //assert
            Assert.AreEqual(callCount, 1);
            Assert.ReferenceEquals(myPatch, receivedPatch);
        }
    }
}
