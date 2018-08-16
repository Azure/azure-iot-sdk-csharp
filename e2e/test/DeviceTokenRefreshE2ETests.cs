// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class DeviceTokenRefreshE2ETests : IDisposable
    {
        private const string DevicePrefix = "E2E_Message_TokenRefresh_";

        private readonly ConsoleEventListener _listener;

        public DeviceTokenRefreshE2ETests()
        {
            _listener = new ConsoleEventListener("Microsoft-Azure-");
        }

        [TestMethod]
        public async Task DeviceClient_TokenIsRefreshed_Ok_Http()
        {
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Http1).ConfigureAwait(false);
        }

        [Ignore]    // TODO: #263
        [TestMethod]
        public async Task DeviceClient_TokenIsRefreshed_Ok_Amqp()
        {
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_TokenIsRefreshed_Fails_Mqtt()
        {
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Mqtt).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_TokenConnectionDoubleRelease_Ok()
        {
            string deviceConnectionString = null;
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            deviceConnectionString = testDevice.ConnectionString;

            var config = new Configuration.IoTHub.DeviceConnectionStringParser(deviceConnectionString);
            string iotHub = config.IoTHub;
            string deviceId = config.DeviceID;
            string key = config.SharedAccessKey;

            SharedAccessSignatureBuilder builder = new SharedAccessSignatureBuilder()
            {
                Key = key,
                TimeToLive = new TimeSpan(0, 10, 0),
                Target = $"{iotHub}/devices/{WebUtility.UrlEncode(deviceId)}",
            };

            DeviceAuthenticationWithToken auth = new DeviceAuthenticationWithToken(deviceId, builder.ToSignature());

            using (DeviceClient iotClient = DeviceClient.Create(iotHub, auth, Client.TransportType.Amqp_Tcp_Only))
            {
                Console.WriteLine("DeviceClient OpenAsync.");
                await iotClient.OpenAsync().ConfigureAwait(false);
                Console.WriteLine("DeviceClient SendEventAsync.");
                await iotClient.SendEventAsync(new Client.Message(Encoding.UTF8.GetBytes("TestMessage"))).ConfigureAwait(false);
                Console.WriteLine("DeviceClient CloseAsync.");
                await iotClient.CloseAsync().ConfigureAwait(false);   // First release
            } // Second release
        }

        private async Task DeviceClient_TokenIsRefreshed_Internal(Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            int ttl = 6;
            int buffer = 50;

            Device device = testDevice.Identity;

            var refresher = new TestTokenRefresher(
                device.Id, 
                device.Authentication.SymmetricKey.PrimaryKey, 
                ttl, 
                buffer);

            DeviceClient deviceClient = 
                DeviceClient.Create(testDevice.IoTHubHostName, refresher, transport);

            var message = new Client.Message(Encoding.UTF8.GetBytes("Hello"));

            // Create the first Token.
            Console.WriteLine($"[{DateTime.UtcNow}] OpenAsync");
            await deviceClient.OpenAsync().ConfigureAwait(false);
            Console.WriteLine($"[{DateTime.UtcNow}] SendEventAsync (1)");
            await deviceClient.SendEventAsync(message).ConfigureAwait(false);

            int countAfterOpenAndFirstSend = refresher.SafeCreateNewTokenCallCount;
            Assert.IsTrue(countAfterOpenAndFirstSend >= 1, $"[{DateTime.UtcNow}] Token should have been refreshed at least once.");

            Console.WriteLine($"[{DateTime.UtcNow}] Waiting {ttl} seconds.");

            // Wait for the Token to expire.
            await Task.Delay(ttl * 1000).ConfigureAwait(false);

            Console.WriteLine($"[{DateTime.UtcNow}] SendEventAsync (2)");
            await deviceClient.SendEventAsync(message).ConfigureAwait(false);

            // Ensure that the token was refreshed.
            if (transport == Client.TransportType.Mqtt)
            {
                // This is not currently supported for MQTT unless the connection is dropped and re-established.
                Assert.IsTrue(
                    refresher.SafeCreateNewTokenCallCount >= countAfterOpenAndFirstSend,
                    $"[{DateTime.UtcNow}] Token should have been refreshed after TTL expired.");
            }
            else
            {
                Assert.IsTrue(
                    refresher.SafeCreateNewTokenCallCount >= countAfterOpenAndFirstSend + 1,
                    $"[{DateTime.UtcNow}] Token should have been refreshed after TTL expired.");
            }

            Console.WriteLine($"[{DateTime.UtcNow}] CloseAsync");
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listener.Dispose();
            }
        }

        private class TestTokenRefresher : DeviceAuthenticationWithTokenRefresh
        {
            private int _callCount = 0;
            private string _key;

            public int SafeCreateNewTokenCallCount
            {
                get
                {
                    return _callCount;
                }
            }

            public TestTokenRefresher(string deviceId, string key) : base(deviceId)
            {
                _key = key;
            }

            public TestTokenRefresher(
                string deviceId, 
                string key, 
                int suggestedTimeToLive, 
                int timeBufferPercentage) 
                : base(deviceId, suggestedTimeToLive, timeBufferPercentage)
            {
                _key = key;
            }

            protected override Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLive)
            {
                Console.WriteLine($"[{DateTime.UtcNow}] Refresher: Creating new token {_callCount}");

                var builder = new SharedAccessSignatureBuilder()
                {
                    Key = _key,
                    TimeToLive = TimeSpan.FromSeconds(suggestedTimeToLive),
                    Target = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}/devices/{1}",
                        iotHub,
                        WebUtility.UrlEncode(DeviceId)),
                };

                _callCount++;

                string token = builder.ToSignature();
                Console.WriteLine($"Token: {token}");
                return Task.FromResult(token);
            }
        }
    }
}
