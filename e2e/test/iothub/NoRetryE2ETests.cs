// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub")]
    public class NoRetryE2ETests : E2EMsTestBase
    {
        private static readonly string _devicePrefix = $"{nameof(NoRetryE2ETests)}_";

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [TestCategory("FaultInjection")]
        public async Task FaultInjection_NoRetry_NoRecovery_OpenAsync()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientAmqpSettings()));

            Logger.Trace($"{nameof(FaultInjection_NoRetry_NoRecovery_OpenAsync)}: deviceId={testDevice.Id}");
            deviceClient.SetRetryPolicy(new NoRetry());

            var connectionStatusChange = new Dictionary<ConnectionStatus, int>();
            deviceClient.SetConnectionStatusChangeHandler((connectionStatusInfo) =>
            {
                connectionStatusChange.TryGetValue(connectionStatusInfo.Status, out int count);
                count++;
                connectionStatusChange[connectionStatusInfo.Status] = count;
            });

            Logger.Trace($"{nameof(FaultInjection_NoRetry_NoRecovery_OpenAsync)}: calling OpenAsync...");
            await deviceClient.OpenAsync().ConfigureAwait(false);

            Logger.Trace($"{nameof(FaultInjection_NoRetry_NoRecovery_OpenAsync)}: injecting fault {FaultInjectionConstants.FaultType_Tcp}...");
            await FaultInjection
                .ActivateFaultInjectionAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    deviceClient,
                    Logger)
                .ConfigureAwait(false);

            await Task.Delay(FaultInjection.DefaultFaultDelay).ConfigureAwait(false);

            Logger.Trace($"{nameof(FaultInjection_NoRetry_NoRecovery_OpenAsync)}: waiting fault injection occurs...");
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < FaultInjection.LatencyTimeBuffer)
            {
                if (connectionStatusChange.ContainsKey(ConnectionStatus.Disconnected))
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
            sw.Reset();

            var lastConnectionStatus = deviceClient.ConnectionStatusInfo.Status;
            var lastConnectionStatusChangeReason = deviceClient.ConnectionStatusInfo.ChangeReason;

            lastConnectionStatus.Should().Be(ConnectionStatus.Disconnected, $"Expected device to be {ConnectionStatus.Disconnected} but was {lastConnectionStatus}.");
            lastConnectionStatusChangeReason.Should().Be(ConnectionStatusChangeReason.RetryExpired, $"Expected device to be {ConnectionStatusChangeReason.RetryExpired} but was {lastConnectionStatusChangeReason}.");
            connectionStatusChange.Should().NotContainKey(ConnectionStatus.DisconnectedRetrying, $"Shouldn't get {ConnectionStatus.DisconnectedRetrying} status change.");
            int connected = connectionStatusChange[ConnectionStatus.Connected];
            connected.Should().Be(1, $"Should get {ConnectionStatus.Connected} once but got it {connected} times.");
            int disconnected = connectionStatusChange[ConnectionStatus.Disconnected];
            disconnected.Should().Be(1, $"Should get {ConnectionStatus.Disconnected} once but got it {disconnected} times.");
        }

        [TestCategory("E2E")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DuplicateDevice_NoRetry_NoPingpong_OpenAsync()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);

            Logger.Trace($"{nameof(DuplicateDevice_NoRetry_NoPingpong_OpenAsync)}: 2 device client instances with the same deviceId={testDevice.Id}.");

            var options = new IotHubClientOptions(new IotHubClientAmqpSettings());
            using IotHubDeviceClient deviceClient1 = testDevice.CreateDeviceClient(options);
            using IotHubDeviceClient deviceClient2 = testDevice.CreateDeviceClient(options);

            Logger.Trace($"{nameof(DuplicateDevice_NoRetry_NoPingpong_OpenAsync)}: set device client instance 1 to no retry.");
            deviceClient1.SetRetryPolicy(new NoRetry());

            var connectionStatusChangeDevice1 = new Dictionary<ConnectionStatus, int>();
            deviceClient1.SetConnectionStatusChangeHandler((connectionStatusInfo) =>
            {
                connectionStatusChangeDevice1.TryGetValue(connectionStatusInfo.Status, out int count);
                count++;
                connectionStatusChangeDevice1[connectionStatusInfo.Status] = count;
            });

            var connectionStatusChangeDevice2 = new Dictionary<ConnectionStatus, int>();
            deviceClient2.SetConnectionStatusChangeHandler((connectionStatusInfo) =>
            {
                connectionStatusChangeDevice2.TryGetValue(connectionStatusInfo.Status, out int count);
                count++;
                connectionStatusChangeDevice2[connectionStatusInfo.Status] = count;
            });

            Logger.Trace($"{nameof(DuplicateDevice_NoRetry_NoPingpong_OpenAsync)}: device client instance 1 calling OpenAsync...");
            await deviceClient1.OpenAsync().ConfigureAwait(false);
            var response = new Client.DirectMethodResponse()
            {
                Status = 200,
            };

            await deviceClient1
                .SetMethodHandlerAsync(
                    (methodRequest, userContext) => Task.FromResult(response),
                    deviceClient1)
                .ConfigureAwait(false);

            Logger.Trace($"{nameof(DuplicateDevice_NoRetry_NoPingpong_OpenAsync)}: device client instance 2 calling OpenAsync...");
            await deviceClient2.OpenAsync().ConfigureAwait(false);
            await deviceClient2
                .SetMethodHandlerAsync(
                    (methodRequest, userContext) => Task.FromResult(response),
                    deviceClient2)
                .ConfigureAwait(false);

            Logger.Trace($"{nameof(DuplicateDevice_NoRetry_NoPingpong_OpenAsync)}: waiting device client instance 1 to be kicked off...");
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < FaultInjection.LatencyTimeBuffer)
            {
                if (connectionStatusChangeDevice1.ContainsKey(ConnectionStatus.Disconnected))
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
            sw.Reset();

            var lastConnectionStatusDevice1 = deviceClient1.ConnectionStatusInfo.Status;
            lastConnectionStatusDevice1.Should().Be(ConnectionStatus.Disconnected, $"Excpected device 1 to be {ConnectionStatus.Disconnected} but was {lastConnectionStatusDevice1}.");
            connectionStatusChangeDevice1.Should().NotContainKey(ConnectionStatus.DisconnectedRetrying, $"Shouldn't get {ConnectionStatus.DisconnectedRetrying} status change.");
            int connected = connectionStatusChangeDevice1[ConnectionStatus.Connected];
            connected.Should().Be(1, $"Should get {ConnectionStatus.Connected} once but got it {connected} times.");
            int disconnected = connectionStatusChangeDevice1[ConnectionStatus.Disconnected];
            disconnected.Should().Be(1, $"Should get {ConnectionStatus.Disconnected} once but got it {disconnected} times.");

            var lastConnectionStatusDevice2 = deviceClient2.ConnectionStatusInfo.Status;
            lastConnectionStatusDevice2.Should().Be(ConnectionStatus.Connected, $"Expected device 2 to be {ConnectionStatus.Connected} but was {lastConnectionStatusDevice2}.");
        }
    }
}
