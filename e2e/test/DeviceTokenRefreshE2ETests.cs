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
        private const string DevicePrefix = "E2E_DeviceTokenRefresh_";

        private readonly ConsoleEventListener _listener;
        private readonly TestLogging _log;

        public DeviceTokenRefreshE2ETests()
        {
            _listener = TestConfig.StartEventListener();
            _log = TestLogging.GetInstance();
        }

        [TestMethod]
        public async Task DeviceClient_TokenIsRefreshed_Ok_Http()
        {
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_TokenIsRefreshed_Ok_Amqp()
        {
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_TokenIsRefreshed_Ok_Mqtt()
        {
            // The IoT Hub service allows tokens expired < 5 minutes ago to be used during CONNECT.
            // After connecting with such an expired token, the service has an allowance of 5 more minutes before dropping the TCP connection.
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Mqtt, 6 * 60).ConfigureAwait(false);
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

        private async Task DeviceClient_TokenIsRefreshed_Internal(Client.TransportType transport, int ttl = 6)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            int buffer = 50;

            Device device = testDevice.Identity;

            var refresher = new TestTokenRefresher(
                device.Id, 
                device.Authentication.SymmetricKey.PrimaryKey, 
                ttl, 
                buffer,
                transport);

            using (DeviceClient deviceClient = DeviceClient.Create(testDevice.IoTHubHostName, refresher, transport))
            {
                if (transport == Client.TransportType.Mqtt)
                {
                    deviceClient.SetConnectionStatusChangesHandler((ConnectionStatus status, ConnectionStatusChangeReason reason) =>
                    {
                        _log.WriteLine($"{nameof(ConnectionStatusChangesHandler)}: {status}; {reason}");
                    });
                }

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
                Assert.IsTrue(
                    refresher.SafeCreateNewTokenCallCount >= countAfterOpenAndFirstSend + 1,
                    $"[{DateTime.UtcNow}] Token should have been refreshed after TTL expired.");

                Console.WriteLine($"[{DateTime.UtcNow}] CloseAsync");
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        private class TestTokenRefresher : DeviceAuthenticationWithTokenRefresh
        {
            private int _callCount = 0;
            private string _key;
            private Client.TransportType _transport;

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
                int timeBufferPercentage,
                Client.TransportType transport) 
                : base(deviceId, suggestedTimeToLive, timeBufferPercentage)
            {
                _key = key;
                _transport = transport;
            }

            protected override Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLive)
            {
                Console.WriteLine($"[{DateTime.UtcNow}] Refresher: Creating new token {_callCount}");

                if (_transport == Client.TransportType.Mqtt)
                {
                    suggestedTimeToLive = -4 * 60 - 59; // server side time allowance.
                }

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
