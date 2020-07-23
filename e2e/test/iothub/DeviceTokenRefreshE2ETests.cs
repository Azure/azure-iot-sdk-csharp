// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class DeviceTokenRefreshE2ETests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(DeviceTokenRefreshE2ETests)}_";

        private readonly ConsoleEventListener _listener;
        private readonly TestLogger _log;

        private const int IoTHubServerTimeAllowanceSeconds = 5 * 60;

        public DeviceTokenRefreshE2ETests()
        {
            _listener = TestConfig.StartEventListener();
            _log = TestLogger.GetInstance();
        }

        [TestMethod]
        [ExpectedException(typeof(DeviceNotFoundException))]
        public async Task DeviceClient_Not_Exist_AMQP()
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            var config = new Configuration.IoTHub.DeviceConnectionStringParser(testDevice.ConnectionString);
            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString($"HostName={config.IoTHub};DeviceId=device_id_not_exist;SharedAccessKey={config.SharedAccessKey}", Client.TransportType.Amqp_Tcp_Only))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task DeviceClient_Bad_Credentials_AMQP()
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            var config = new Configuration.IoTHub.DeviceConnectionStringParser(testDevice.ConnectionString);
            string invalidKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid_key"));
            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString($"HostName={config.IoTHub};DeviceId={config.DeviceID};SharedAccessKey={invalidKey}", Client.TransportType.Amqp_Tcp_Only))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        [TestCategory("Flaky")]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_TokenIsRefreshed_Ok_Http()
        {
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_TokenIsRefreshed_Ok_Amqp()
        {
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_TokenIsRefreshed_Ok_Mqtt()
        {
            // The IoT Hub service allows tokens expired < 5 minutes ago to be used during CONNECT.
            // After connecting with such an expired token, the service has an allowance of 5 more minutes before dropping the TCP connection.
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Mqtt, IoTHubServerTimeAllowanceSeconds + 60).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_TokenConnectionDoubleRelease_Ok()
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            string deviceConnectionString = testDevice.ConnectionString;

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

            using (DeviceClient deviceClient = DeviceClient.Create(iotHub, auth, Client.TransportType.Amqp_Tcp_Only))
            {
                _log.Trace($"Created {nameof(DeviceClient)} ID={TestLogger.IdOf(deviceClient)}");

                Console.WriteLine("DeviceClient OpenAsync.");
                await deviceClient.OpenAsync().ConfigureAwait(false);
                Console.WriteLine("DeviceClient SendEventAsync.");
                await deviceClient.SendEventAsync(new Client.Message(Encoding.UTF8.GetBytes("TestMessage"))).ConfigureAwait(false);
                Console.WriteLine("DeviceClient CloseAsync.");
                await deviceClient.CloseAsync().ConfigureAwait(false);   // First release
            } // Second release
        }

        private async Task DeviceClient_TokenIsRefreshed_Internal(Client.TransportType transport, int ttl = 20)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            int buffer = 50;
            Device device = testDevice.Device;
            SemaphoreSlim deviceDisconnected = new SemaphoreSlim(0);

            var refresher = new TestTokenRefresher(
                device.Id,
                device.Authentication.SymmetricKey.PrimaryKey,
                ttl,
                buffer,
                transport);

            using (DeviceClient deviceClient = DeviceClient.Create(testDevice.IoTHubHostName, refresher, transport))
            {
                _log.Trace($"Created {nameof(DeviceClient)} ID={TestLogger.IdOf(deviceClient)}");

                if (transport == Client.TransportType.Mqtt)
                {
                    deviceClient.SetConnectionStatusChangesHandler((ConnectionStatus status, ConnectionStatusChangeReason reason) =>
                    {
                        _log.Trace($"{nameof(ConnectionStatusChangesHandler)}: {status}; {reason}");
                        if (status == ConnectionStatus.Disconnected_Retrying || status == ConnectionStatus.Disconnected) deviceDisconnected.Release();
                    });
                }

                var message = new Client.Message(Encoding.UTF8.GetBytes("Hello"));

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ttl * 10)))
                {
                    try
                    {
                        // Create the first Token.
                        Console.WriteLine($"[{DateTime.UtcNow}] OpenAsync");
                        await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);

                        Console.WriteLine($"[{DateTime.UtcNow}] SendEventAsync (1)");
                        await deviceClient.SendEventAsync(message, cts.Token).ConfigureAwait(false);
                        await refresher.WaitForTokenRefreshAsync(cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex)
                    {
                        Assert.Fail($"{TestLogger.IdOf(deviceClient)} did not get the initial token. {ex}");
                        throw;
                    }

                    // Wait for the Token to expire.
                    if (transport == Client.TransportType.Http1)
                    {
                        float waitTime = (float)ttl * ((float)buffer / 100) + 1;
                        Console.WriteLine($"[{DateTime.UtcNow}] Waiting {waitTime} seconds.");
                        await Task.Delay(TimeSpan.FromSeconds(waitTime)).ConfigureAwait(false);
                    }
                    else if (transport == Client.TransportType.Mqtt)
                    {
                        Console.WriteLine($"[{DateTime.UtcNow}] Waiting for device disconnect.");
                        await deviceDisconnected.WaitAsync(cts.Token).ConfigureAwait(false);
                    }

                    try
                    {
                        Console.WriteLine($"[{DateTime.UtcNow}] SendEventAsync (2)");
                        await deviceClient.SendEventAsync(message, cts.Token).ConfigureAwait(false);
                        await refresher.WaitForTokenRefreshAsync(cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex)
                    {
                        Assert.Fail($"{TestLogger.IdOf(deviceClient)} did not refresh token after {refresher.DetectedRefreshInterval}. {ex}");
                        throw;
                    }

                    // Ensure that the token was refreshed.
                    Console.WriteLine($"[{DateTime.UtcNow}] Token was refreshed after {refresher.DetectedRefreshInterval} (ttl = {ttl} seconds).");
                    Assert.IsTrue(
                        refresher.DetectedRefreshInterval.TotalSeconds < (float)ttl * (1 + (float)buffer / 100), // Wait for more than what we expect.
                        $"Token was refreshed after {refresher.DetectedRefreshInterval} although ttl={ttl} seconds.");

                    Console.WriteLine($"[{DateTime.UtcNow}] CloseAsync");
                    await deviceClient.CloseAsync().ConfigureAwait(false);
                }
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
            private string _key;
            private Client.TransportType _transport;
            private Stopwatch _stopwatch = new Stopwatch();
            private SemaphoreSlim _tokenRefreshSemaphore = new SemaphoreSlim(0);
            private int _counter;

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

            public TimeSpan DetectedRefreshInterval => _stopwatch.Elapsed;

            public Task WaitForTokenRefreshAsync(CancellationToken cancellationToken)
            {
                return _tokenRefreshSemaphore.WaitAsync(cancellationToken);
            }

            protected override Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLive)
            {
                Console.WriteLine($"[{DateTime.UtcNow}] Refresher: Creating new token.");

                if (_transport == Client.TransportType.Mqtt)
                {
                    suggestedTimeToLive = -IoTHubServerTimeAllowanceSeconds + 30; // Create an expired token.
                }

                var builder = new SharedAccessSignatureBuilder
                {
                    Key = _key,
                    TimeToLive = TimeSpan.FromSeconds(suggestedTimeToLive),
                    Target = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}/devices/{1}",
                        iotHub,
                        WebUtility.UrlEncode(DeviceId)),
                };

                string token = builder.ToSignature();
                Console.WriteLine($"Token: {token}");

                _tokenRefreshSemaphore.Release();
                _counter++;

                if (_counter == 1) _stopwatch.Start();
                else _stopwatch.Stop();

                return Task.FromResult(token);
            }
        }
    }
}
