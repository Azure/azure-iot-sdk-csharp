﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
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
    public class DeviceTokenRefreshE2ETests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(DeviceTokenRefreshE2ETests)}_";

        private const int IoTHubServerTimeAllowanceSeconds = 5 * 60;

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [ExpectedException(typeof(DeviceNotFoundException))]
        public async Task DeviceClient_Not_Exist_AMQP()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            var config = new TestConfiguration.IotHub.ConnectionStringParser(testDevice.ConnectionString);
            using (var deviceClient = DeviceClient.CreateFromConnectionString($"HostName={config.IotHubHostName};DeviceId=device_id_not_exist;SharedAccessKey={config.SharedAccessKey}", Client.TransportType.Amqp_Tcp_Only))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task DeviceClient_Bad_Credentials_AMQP()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            var config = new TestConfiguration.IotHub.ConnectionStringParser(testDevice.ConnectionString);
            string invalidKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid_key"));
            using (var deviceClient = DeviceClient.CreateFromConnectionString($"HostName={config.IotHubHostName};DeviceId={config.DeviceId};SharedAccessKey={invalidKey}", Client.TransportType.Amqp_Tcp_Only))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        [Timeout(TokenRefreshTestTimeoutMilliseconds)]
        [TestCategory("Flaky")]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_TokenIsRefreshed_Ok_Http()
        {
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TokenRefreshTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_TokenIsRefreshed_Ok_Amqp()
        {
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TokenRefreshTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_TokenIsRefreshed_Ok_Mqtt()
        {
            // The IoT hub service allows tokens expired < 5 minutes ago to be used during CONNECT.
            // After connecting with such an expired token, the service has an allowance of 5 more minutes before dropping the TCP connection.
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Mqtt, IoTHubServerTimeAllowanceSeconds + 60).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceClient_TokenConnectionDoubleRelease_Ok()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            string deviceConnectionString = testDevice.ConnectionString;

            var config = new TestConfiguration.IotHub.ConnectionStringParser(deviceConnectionString);
            string iotHub = config.IotHubHostName;
            string deviceId = config.DeviceId;
            string key = config.SharedAccessKey;

            var builder = new SharedAccessSignatureBuilder()
            {
                Key = key,
                TimeToLive = new TimeSpan(0, 10, 0),
                Target = $"{iotHub}/devices/{WebUtility.UrlEncode(deviceId)}",
            };

            var auth = new DeviceAuthenticationWithToken(deviceId, builder.ToSignature());

            using var deviceClient = DeviceClient.Create(iotHub, auth, Client.TransportType.Amqp_Tcp_Only);
            VerboseTestLogger.WriteLine($"{deviceId}: Created {nameof(DeviceClient)}");

            VerboseTestLogger.WriteLine($"{deviceId}: DeviceClient OpenAsync.");
            await deviceClient.OpenAsync().ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{deviceId}: DeviceClient SendEventAsync.");
            using var testMessage = new Client.Message(Encoding.UTF8.GetBytes("TestMessage"));
            await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{deviceId}: DeviceClient CloseAsync.");
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        // The easiest way to test that sas tokens expire with custom expiration time via the CreateFromConnectionString flow is
        // by initializing a DeviceClient instance over Mqtt (since sas token expiration over Mqtt is accompanied by a disconnect).
        [TestMethod]
        [Timeout(TokenRefreshTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task DeviceClient_CreateFromConnectionString_TokenIsRefreshed_Mqtt()
        {
            var sasTokenTimeToLive = TimeSpan.FromSeconds(10);
            int sasTokenRenewalBuffer = 50;
            using var deviceDisconnected = new SemaphoreSlim(0);

            int operationTimeoutInMilliseconds = (int)sasTokenTimeToLive.TotalMilliseconds * 2;

            // Service allows a buffer time of upto 10mins before dropping connections that are authenticated with an expired sas tokens.
            using var tokenRefreshCts = new CancellationTokenSource((int)(sasTokenTimeToLive.TotalMilliseconds * 2 + TimeSpan.FromMinutes(10).TotalMilliseconds));

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            var options = new ClientOptions
            {
                SasTokenTimeToLive = sasTokenTimeToLive,
                SasTokenRenewalBuffer = sasTokenRenewalBuffer,
            };

            using DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Mqtt, options);
            VerboseTestLogger.WriteLine($"Created {nameof(DeviceClient)} instance for {testDevice.Id}.");

            deviceClient.SetConnectionStatusChangesHandler((ConnectionStatus status, ConnectionStatusChangeReason reason) =>
            {
                VerboseTestLogger.WriteLine($"{nameof(ConnectionStatusChangesHandler)}: {status}; {reason}");
                if (status == ConnectionStatus.Disconnected_Retrying || status == ConnectionStatus.Disconnected)
                {
                    deviceDisconnected.Release();
                }
            });
            deviceClient.OperationTimeoutInMilliseconds = (uint)operationTimeoutInMilliseconds;

            using var message = new Client.Message(Encoding.UTF8.GetBytes("Hello"));

            VerboseTestLogger.WriteLine($"[{testDevice.Id}]: SendEventAsync (1)");
            await deviceClient.SendEventAsync(message).ConfigureAwait(false);

            // Wait for the Token to expire.
            VerboseTestLogger.WriteLine($"[{testDevice.Id}]: Waiting for device disconnect.");
            await deviceDisconnected.WaitAsync(tokenRefreshCts.Token).ConfigureAwait(false);

            try
            {
                VerboseTestLogger.WriteLine($"[{testDevice.Id}]: SendEventAsync (2)");
                await deviceClient.SendEventAsync(message).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail($"{testDevice.Id} did not refresh token after expected ttl of {sasTokenTimeToLive}: {ex}");
                throw;
            }
        }

        private async Task DeviceClient_TokenIsRefreshed_Internal(Client.TransportType transport, int ttl = 20)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            int buffer = 50;
            Device device = testDevice.Device;
            using var deviceDisconnected = new SemaphoreSlim(0);

            using var refresher = new TestTokenRefresher(
                device.Id,
                device.Authentication.SymmetricKey.PrimaryKey,
                ttl,
                buffer,
                transport);

            using var deviceClient = DeviceClient.Create(TestDevice.IotHubHostName, refresher, transport);
            VerboseTestLogger.WriteLine($"Created {nameof(DeviceClient)}");

            if (transport == Client.TransportType.Mqtt)
            {
                deviceClient.SetConnectionStatusChangesHandler((ConnectionStatus status, ConnectionStatusChangeReason reason) =>
                {
                    VerboseTestLogger.WriteLine($"{nameof(ConnectionStatusChangesHandler)}: {status}; {reason}");
                    if (status == ConnectionStatus.Disconnected_Retrying || status == ConnectionStatus.Disconnected)
                    {
                        deviceDisconnected.Release();
                    }
                });
            }

            using var message = new Client.Message(Encoding.UTF8.GetBytes("Hello"));

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ttl * 10));
            try
            {
                // Create the first Token.
                VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] OpenAsync");
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);

                VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] SendEventAsync (1)");
                await deviceClient.SendEventAsync(message, cts.Token).ConfigureAwait(false);
                await refresher.WaitForTokenRefreshAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail($"Did not get the initial token. {ex}");
                throw;
            }

            // Wait for the Token to expire.
            if (transport == Client.TransportType.Http1)
            {
                float waitTime = (float)ttl * ((float)buffer / 100) + 1;
                VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] Waiting {waitTime} seconds.");
                await Task.Delay(TimeSpan.FromSeconds(waitTime)).ConfigureAwait(false);
            }
            else if (transport == Client.TransportType.Mqtt)
            {
                VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] Waiting for device disconnect.");
                await deviceDisconnected.WaitAsync(cts.Token).ConfigureAwait(false);
            }

            try
            {
                VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] SendEventAsync (2)");
                await deviceClient.SendEventAsync(message, cts.Token).ConfigureAwait(false);
                await refresher.WaitForTokenRefreshAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail($"Did not refresh token after {refresher.DetectedRefreshInterval}. {ex}");
                throw;
            }

            // Ensure that the token was refreshed.
            VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] Token was refreshed after {refresher.DetectedRefreshInterval} (ttl = {ttl} seconds).");
            Assert.IsTrue(
                refresher.DetectedRefreshInterval.TotalSeconds < (float)ttl * (1 + (float)buffer / 100), // Wait for more than what we expect.
                $"Token was refreshed after {refresher.DetectedRefreshInterval} although ttl={ttl} seconds.");

            VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] CloseAsync");
            await deviceClient.CloseAsync().ConfigureAwait(false);
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

            ///<inheritdoc/>
            protected override Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLive)
            {
                VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] Refresher: Creating new token.");

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
                VerboseTestLogger.WriteLine($"Token: {token}");

                _tokenRefreshSemaphore.Release();
                _counter++;

                if (_counter == 1)
                {
                    _stopwatch.Start();
                }
                else
                {
                    _stopwatch.Stop();
                }

                return Task.FromResult(token);
            }
        }
    }
}
