// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
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
        public async Task IotHubDeviceClient_Not_Exist_AMQP()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            var config = new TestConfiguration.IotHub.ConnectionStringParser(testDevice.ConnectionString);
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings());
            await using var deviceClient = new IotHubDeviceClient(
                $"HostName={config.IotHubHostName};DeviceId=device_id_not_exist;SharedAccessKey={config.SharedAccessKey}",
                options);

            // act
            Func<Task> act = async () =>
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
            };

            //assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.DeviceNotFound);
            error.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_Bad_Credentials_AMQP()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            var config = new TestConfiguration.IotHub.ConnectionStringParser(testDevice.ConnectionString);
            string invalidKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid_key"));
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings());
            await using var deviceClient = new IotHubDeviceClient(
                $"HostName={config.IotHubHostName};DeviceId={config.DeviceId};SharedAccessKey={invalidKey}",
                options);

            // act
            Func<Task> act = async () =>
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.Unauthorized);
            error.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        [Timeout(TokenRefreshTestTimeoutMilliseconds)]
        [TestCategory("Flaky")]
        [TestCategory("LongRunning")]
        public async Task IotHubDeviceClient_TokenIsRefreshed_Ok_Amqp()
        {
            await IotHubDeviceClient_TokenIsRefreshed_Internal(new IotHubClientAmqpSettings(), TimeSpan.FromSeconds(20)).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TokenRefreshTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task IotHubDeviceClient_TokenIsRefreshed_Ok_Mqtt()
        {
            // The IoT hub service allows tokens expired < 5 minutes ago to be used during CONNECT.
            // After connecting with such an expired token, the service has an allowance of 5 more minutes before dropping the TCP connection.
            await IotHubDeviceClient_TokenIsRefreshed_Internal(new IotHubClientMqttSettings(), TimeSpan.FromSeconds(IoTHubServerTimeAllowanceSeconds + 60)).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_TokenConnectionDoubleRelease_Ok()
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

            var auth = new ClientAuthenticationWithSharedAccessSignature(builder.ToSignature(), deviceId);

            await using var deviceClient = new IotHubDeviceClient(iotHub, auth, new IotHubClientOptions(new IotHubClientAmqpSettings()));
            VerboseTestLogger.WriteLine($"{deviceId}: Created {nameof(IotHubDeviceClient)}");

            VerboseTestLogger.WriteLine($"{deviceId}: DeviceClient OpenAsync.");
            await deviceClient.OpenAsync().ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{deviceId}: DeviceClient SendTelemetryAsync.");
            var testMessage = new TelemetryMessage("TestMessage");
            await deviceClient.SendTelemetryAsync(testMessage).ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{deviceId}: DeviceClient CloseAsync.");
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        // The easiest way to test that sas tokens expire with custom expiration time via the CreateFromConnectionString flow is
        // by initializing a DeviceClient instance over Mqtt (since sas token expiration over Mqtt is accompanied by a disconnect).
        [TestMethod]
        [Timeout(TokenRefreshTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task IotHubDeviceClient_CreateFromConnectionString_TokenIsRefreshed_Mqtt()
        {
            var sasTokenTimeToLive = TimeSpan.FromSeconds(10);
            int sasTokenRenewalBuffer = 50;
            using var deviceDisconnected = new SemaphoreSlim(0);

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            var auth = new ClientAuthenticationWithSharedAccessKeyRefresh(testDevice.ConnectionString, sasTokenTimeToLive, sasTokenRenewalBuffer);

            var options = new IotHubClientOptions(new IotHubClientMqttSettings());

            await using var deviceClient = new IotHubDeviceClient(testDevice.IotHubHostName, auth, options);
            VerboseTestLogger.WriteLine($"Created {nameof(IotHubDeviceClient)} instance for {testDevice.Id}.");

            void ConnectionStatusChangeHandler(ConnectionStatusInfo connectionStatusInfo)
            {
                ConnectionStatus status = connectionStatusInfo.Status;
                ConnectionStatusChangeReason reason = connectionStatusInfo.ChangeReason;
                VerboseTestLogger.WriteLine($"{nameof(DeviceTokenRefreshE2ETests)}: {status}; {reason}");
                if (status == ConnectionStatus.DisconnectedRetrying || status == ConnectionStatus.Disconnected)
                {
                    deviceDisconnected.Release();
                }
            };

            deviceClient.ConnectionStatusChangeCallback = ConnectionStatusChangeHandler;

            var message = new TelemetryMessage("Hello");

            VerboseTestLogger.WriteLine($"[{testDevice.Id}]: SendTelemetryAsync (1)");
            var timeout = TimeSpan.FromSeconds(sasTokenTimeToLive.TotalSeconds * 2);
            using var cts1 = new CancellationTokenSource(timeout);
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.SendTelemetryAsync(message, cts1.Token).ConfigureAwait(false);

            // Wait for the Token to expire.

            // Service allows a buffer time of upto 10mins before dropping connections that are authenticated with an expired sas tokens.
            using var tokenRefreshCts = new CancellationTokenSource((int)(sasTokenTimeToLive.TotalMilliseconds * 2 + TimeSpan.FromMinutes(10).TotalMilliseconds));

            VerboseTestLogger.WriteLine($"[{testDevice.Id}]: Waiting for device disconnect.");
            await deviceDisconnected.WaitAsync(tokenRefreshCts.Token).ConfigureAwait(false);

            // Test that the client is able to send messages
            VerboseTestLogger.WriteLine($"[{testDevice.Id}]: SendTelemetryAsync (2)");
            using var cts2 = new CancellationTokenSource(timeout);
            await deviceClient.SendTelemetryAsync(message, cts2.Token).ConfigureAwait(false);
        }

        private async Task IotHubDeviceClient_TokenIsRefreshed_Internal(IotHubClientTransportSettings transportSettings, TimeSpan ttl)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            int buffer = 50;
            Device device = testDevice.Device;
            using var deviceDisconnected = new SemaphoreSlim(0);

            var refresher = new TestTokenRefresher(
                device.Id,
                device.Authentication.SymmetricKey.PrimaryKey,
                ttl,
                buffer,
                transportSettings);

            await using var deviceClient = new IotHubDeviceClient(testDevice.IotHubHostName, refresher, new IotHubClientOptions(transportSettings));
            VerboseTestLogger.WriteLine($"Created {nameof(IotHubDeviceClient)}");

            if (transportSettings is IotHubClientMqttSettings
                && transportSettings.Protocol == IotHubClientTransportProtocol.Tcp)
            {
                void ConnectionStatusChangeHandler(ConnectionStatusInfo connectionStatusInfo)
                {
                    ConnectionStatus status = connectionStatusInfo.Status;
                    ConnectionStatusChangeReason reason = connectionStatusInfo.ChangeReason;
                    VerboseTestLogger.WriteLine($"{nameof(DeviceTokenRefreshE2ETests)}: {status}; {reason}");
                    if (status == ConnectionStatus.DisconnectedRetrying || status == ConnectionStatus.Disconnected)
                    {
                        deviceDisconnected.Release();
                    }
                };

                deviceClient.ConnectionStatusChangeCallback = ConnectionStatusChangeHandler;
            }

            var message = new TelemetryMessage("Hello");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ttl.TotalSeconds * 10));
            try
            {
                // Create the first Token.
                VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] OpenAsync");
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);

                VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] SendTelemetryAsync (1)");
                await deviceClient.SendTelemetryAsync(message, cts.Token).ConfigureAwait(false);
                await refresher.WaitForTokenRefreshAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail($"Did not get the initial token. {ex}");
                throw;
            }

            // Wait for the Token to expire.
            if (transportSettings is IotHubClientHttpSettings)
            {
                float waitTime = (float)(ttl.TotalSeconds * ((float)buffer / 100) + 1);
                VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] Waiting {waitTime} seconds.");
                await Task.Delay(TimeSpan.FromSeconds(waitTime)).ConfigureAwait(false);
            }
            else if (transportSettings is IotHubClientMqttSettings
                && transportSettings.Protocol == IotHubClientTransportProtocol.Tcp)
            {
                VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] Waiting for device disconnect.");
                await deviceDisconnected.WaitAsync(cts.Token).ConfigureAwait(false);
            }

            try
            {
                VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] SendTelemetryAsync (2)");
                await deviceClient.SendTelemetryAsync(message, cts.Token).ConfigureAwait(false);
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
                refresher.DetectedRefreshInterval.TotalSeconds < (float)ttl.TotalSeconds * (1 + (float)buffer / 100), // Wait for more than what we expect.
                $"Token was refreshed after {refresher.DetectedRefreshInterval} although ttl={ttl} seconds.");

            VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] CloseAsync");
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private class TestTokenRefresher : ClientAuthenticationWithTokenRefresh
        {
            private readonly string _key;
            private readonly IotHubClientTransportSettings _transportSettings;
            private readonly Stopwatch _stopwatch = new();
            private readonly SemaphoreSlim _tokenRefreshSemaphore = new(0);
            private int _counter;

            public TestTokenRefresher(string deviceId, string key) : base(deviceId)
            {
                _key = key;
            }

            public TestTokenRefresher(
                string deviceId,
                string key,
                TimeSpan suggestedTimeToLive,
                int timeBufferPercentage,
                IotHubClientTransportSettings transportSettings)
                : base(
                      deviceId: deviceId,
                      suggestedTimeToLive: suggestedTimeToLive,
                      timeBufferPercentage: timeBufferPercentage)
            {
                _key = key;
                _transportSettings = transportSettings;
            }

            public TimeSpan DetectedRefreshInterval => _stopwatch.Elapsed;

            public Task WaitForTokenRefreshAsync(CancellationToken cancellationToken)
            {
                return _tokenRefreshSemaphore.WaitAsync(cancellationToken);
            }

            ///<inheritdoc/>
            protected override Task<string> SafeCreateNewTokenAsync(string iotHub, TimeSpan suggestedTimeToLive)
            {
                VerboseTestLogger.WriteLine($"[{DateTime.UtcNow}] Refresher: Creating new token.");

                if (_transportSettings is IotHubClientMqttSettings
                    && _transportSettings.Protocol == IotHubClientTransportProtocol.Tcp)
                {
                    suggestedTimeToLive = TimeSpan.FromSeconds(-IoTHubServerTimeAllowanceSeconds + 30); // Create an expired token.
                }

                var builder = new SharedAccessSignatureBuilder
                {
                    Key = _key,
                    TimeToLive = suggestedTimeToLive,
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
