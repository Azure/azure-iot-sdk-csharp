// Copyright (c) Microsoft. All rights reserved.
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

        [LoggedTestMethod]
        [ExpectedException(typeof(DeviceNotFoundException))]
        public async Task IotHubDeviceClient_Not_Exist_AMQP()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);

            var config = new TestConfiguration.IoTHub.ConnectionStringParser(testDevice.ConnectionString);
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings());
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(
                $"HostName={config.IotHubHostName};DeviceId=device_id_not_exist;SharedAccessKey={config.SharedAccessKey}",
                options);
            await deviceClient.OpenAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task IotHubDeviceClient_Bad_Credentials_AMQP()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);

            var config = new TestConfiguration.IoTHub.ConnectionStringParser(testDevice.ConnectionString);
            string invalidKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid_key"));
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings());
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(
                $"HostName={config.IotHubHostName};DeviceId={config.DeviceID};SharedAccessKey={invalidKey}",
                options);
            await deviceClient.OpenAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Flaky")]
        [TestCategory("LongRunning")]
        public async Task IotHubDeviceClient_TokenIsRefreshed_Ok_Http()
        {
            await IotHubDeviceClient_TokenIsRefreshed_Internal(new Client.IotHubClientHttpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task IotHubDeviceClient_TokenIsRefreshed_Ok_Amqp()
        {
            await IotHubDeviceClient_TokenIsRefreshed_Internal(new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task IotHubDeviceClient_TokenIsRefreshed_Ok_Mqtt()
        {
            // The IoT hub service allows tokens expired < 5 minutes ago to be used during CONNECT.
            // After connecting with such an expired token, the service has an allowance of 5 more minutes before dropping the TCP connection.
            await IotHubDeviceClient_TokenIsRefreshed_Internal(new IotHubClientMqttSettings(), IoTHubServerTimeAllowanceSeconds + 60).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task IotHubDeviceClient_TokenConnectionDoubleRelease_Ok()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);

            string deviceConnectionString = testDevice.ConnectionString;

            var config = new TestConfiguration.IoTHub.ConnectionStringParser(deviceConnectionString);
            string iotHub = config.IotHubHostName;
            string deviceId = config.DeviceID;
            string key = config.SharedAccessKey;

            var builder = new SharedAccessSignatureBuilder()
            {
                Key = key,
                TimeToLive = new TimeSpan(0, 10, 0),
                Target = $"{iotHub}/devices/{WebUtility.UrlEncode(deviceId)}",
            };

            var auth = new DeviceAuthenticationWithToken(deviceId, builder.ToSignature());

            using var deviceClient = IotHubDeviceClient.Create(iotHub, auth, new IotHubClientOptions(new IotHubClientAmqpSettings()));
            Logger.Trace($"{deviceId}: Created {nameof(IotHubDeviceClient)} ID={TestLogger.IdOf(deviceClient)}");

            Logger.Trace($"{deviceId}: DeviceClient OpenAsync.");
            await deviceClient.OpenAsync().ConfigureAwait(false);

            Logger.Trace($"{deviceId}: DeviceClient SendEventAsync.");
            using var testMessage = new Client.Message(Encoding.UTF8.GetBytes("TestMessage"));
            await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);

            Logger.Trace($"{deviceId}: DeviceClient CloseAsync.");
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        // The easiest way to test that sas tokens expire with custom expiration time via the CreateFromConnectionString flow is
        // by initializing a DeviceClient instance over Mqtt (since sas token expiration over Mqtt is accompanied by a disconnect).
        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task IotHubDeviceClient_CreateFromConnectionString_TokenIsRefreshed_Mqtt()
        {
            var sasTokenTimeToLive = TimeSpan.FromSeconds(10);
            int sasTokenRenewalBuffer = 50;
            using var deviceDisconnected = new SemaphoreSlim(0);

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);

            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                SasTokenTimeToLive = sasTokenTimeToLive,
                SasTokenRenewalBuffer = sasTokenRenewalBuffer,
            };

            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            Logger.Trace($"Created {nameof(IotHubDeviceClient)} instance for {testDevice.Id}.");

            deviceClient.SetConnectionStatusChangesHandler((ConnectionStatus status, ConnectionStatusChangeReason reason) =>
            {
                Logger.Trace($"{nameof(ConnectionStatusChangesHandler)}: {status}; {reason}");
                if (status == ConnectionStatus.Disconnected_Retrying || status == ConnectionStatus.Disconnected)
                {
                    deviceDisconnected.Release();
                }
            });

            using var message = new Client.Message(Encoding.UTF8.GetBytes("Hello"));

            Logger.Trace($"[{testDevice.Id}]: SendEventAsync (1)");
            var timeout = TimeSpan.FromSeconds(sasTokenTimeToLive.TotalSeconds * 2);
            using var cts1 = new CancellationTokenSource(timeout);
            await deviceClient.SendEventAsync(message, cts1.Token).ConfigureAwait(false);

            // Wait for the Token to expire.

            // Service allows a buffer time of upto 10mins before dropping connections that are authenticated with an expired sas tokens.
            using var tokenRefreshCts = new CancellationTokenSource((int)(sasTokenTimeToLive.TotalMilliseconds * 2 + TimeSpan.FromMinutes(10).TotalMilliseconds));

            Logger.Trace($"[{testDevice.Id}]: Waiting for device disconnect.");
            await deviceDisconnected.WaitAsync(tokenRefreshCts.Token).ConfigureAwait(false);

            try
            {
                Logger.Trace($"[{testDevice.Id}]: SendEventAsync (2)");
                using var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
                await deviceClient.SendEventAsync(message, cts2.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail($"{testDevice.Id} did not refresh token after expected ttl of {sasTokenTimeToLive}: {ex}");
                throw;
            }
        }

        private async Task IotHubDeviceClient_TokenIsRefreshed_Internal(TransportSettings transportSettings, int ttl = 20)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);

            int buffer = 50;
            Device device = testDevice.Device;
            using var deviceDisconnected = new SemaphoreSlim(0);

            using var refresher = new TestTokenRefresher(
                device.Id,
                device.Authentication.SymmetricKey.PrimaryKey,
                ttl,
                buffer,
                transportSettings,
                Logger);

            using var deviceClient = IotHubDeviceClient.Create(testDevice.IotHubHostName, refresher, new IotHubClientOptions(transportSettings));
            Logger.Trace($"Created {nameof(IotHubDeviceClient)} ID={TestLogger.IdOf(deviceClient)}");

            if (transportSettings is IotHubClientMqttSettings
                && transportSettings.Protocol == TransportProtocol.Tcp)
            {
                deviceClient.SetConnectionStatusChangesHandler((ConnectionStatus status, ConnectionStatusChangeReason reason) =>
                {
                    Logger.Trace($"{nameof(ConnectionStatusChangesHandler)}: {status}; {reason}");
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
                Logger.Trace($"[{DateTime.UtcNow}] OpenAsync");
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);

                Logger.Trace($"[{DateTime.UtcNow}] SendEventAsync (1)");
                await deviceClient.SendEventAsync(message, cts.Token).ConfigureAwait(false);
                await refresher.WaitForTokenRefreshAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail($"{TestLogger.IdOf(deviceClient)} did not get the initial token. {ex}");
                throw;
            }

            // Wait for the Token to expire.
            if (transportSettings is Client.IotHubClientHttpSettings)
            {
                float waitTime = ttl * ((float)buffer / 100) + 1;
                Logger.Trace($"[{DateTime.UtcNow}] Waiting {waitTime} seconds.");
                await Task.Delay(TimeSpan.FromSeconds(waitTime)).ConfigureAwait(false);
            }
            else if (transportSettings is IotHubClientMqttSettings
                && transportSettings.Protocol == TransportProtocol.Tcp)
            {
                    Logger.Trace($"[{DateTime.UtcNow}] Waiting for device disconnect.");
                    await deviceDisconnected.WaitAsync(cts.Token).ConfigureAwait(false);
            }

            try
            {
                Logger.Trace($"[{DateTime.UtcNow}] SendEventAsync (2)");
                await deviceClient.SendEventAsync(message, cts.Token).ConfigureAwait(false);
                await refresher.WaitForTokenRefreshAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail($"{TestLogger.IdOf(deviceClient)} did not refresh token after {refresher.DetectedRefreshInterval}. {ex}");
                throw;
            }

            // Ensure that the token was refreshed.
            Logger.Trace($"[{DateTime.UtcNow}] Token was refreshed after {refresher.DetectedRefreshInterval} (ttl = {ttl} seconds).");
            Assert.IsTrue(
                refresher.DetectedRefreshInterval.TotalSeconds < (float)ttl * (1 + (float)buffer / 100), // Wait for more than what we expect.
                $"Token was refreshed after {refresher.DetectedRefreshInterval} although ttl={ttl} seconds.");

            Logger.Trace($"[{DateTime.UtcNow}] CloseAsync");
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private class TestTokenRefresher : DeviceAuthenticationWithTokenRefresh
        {
            private readonly string _key;
            private readonly TransportSettings _transportSettings;
            private readonly Stopwatch _stopwatch = new();
            private readonly SemaphoreSlim _tokenRefreshSemaphore = new(0);
            private int _counter;

            private readonly MsTestLogger _logger;

            public TestTokenRefresher(string deviceId, string key, MsTestLogger logger) : base(deviceId)
            {
                _key = key;
                _logger = logger;
            }

            public TestTokenRefresher(
                string deviceId,
                string key,
                int suggestedTimeToLive,
                int timeBufferPercentage,
                TransportSettings transportSettings,
                MsTestLogger logger)
                : base(deviceId, suggestedTimeToLive, timeBufferPercentage)
            {
                _key = key;
                _transportSettings = transportSettings;
                _logger = logger;
            }

            public TimeSpan DetectedRefreshInterval => _stopwatch.Elapsed;

            public Task WaitForTokenRefreshAsync(CancellationToken cancellationToken)
            {
                return _tokenRefreshSemaphore.WaitAsync(cancellationToken);
            }

            ///<inheritdoc/>
            protected override Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLive)
            {
                _logger.Trace($"[{DateTime.UtcNow}] Refresher: Creating new token.");

                if (_transportSettings is IotHubClientMqttSettings
                    && _transportSettings.Protocol == TransportProtocol.Tcp)
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
                _logger.Trace($"Token: {token}");

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
