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
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class NoRetryE2ETests : E2EMsTestBase
    {
        private static readonly string _devicePrefix = $"{nameof(NoRetryE2ETests)}_";

        [LoggedTestMethod]
        [TestCategory("FaultInjection")]
        public async Task FaultInjection_NoRetry_NoRecovery_OpenAsync()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new AmqpTransportSettings()));

            Logger.Trace($"{nameof(FaultInjection_NoRetry_NoRecovery_OpenAsync)}: deviceId={testDevice.Id}");
            deviceClient.SetRetryPolicy(new NoRetry());

            ConnectionStatus? lastConnectionStatus = null;
            ConnectionStatusChangeReason? lastConnectionStatusChangeReason = null;
            var connectionStatusChanges = new Dictionary<ConnectionStatus, int>();
            deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                connectionStatusChanges.TryGetValue(status, out int count);
                count++;
                connectionStatusChanges[status] = count;
                lastConnectionStatus = status;
                lastConnectionStatusChangeReason = reason;
            });

            Logger.Trace($"{nameof(FaultInjection_NoRetry_NoRecovery_OpenAsync)}: calling OpenAsync...");
            await deviceClient.OpenAsync().ConfigureAwait(false);

            Logger.Trace($"{nameof(FaultInjection_NoRetry_NoRecovery_OpenAsync)}: injecting fault {FaultInjection.FaultType_Tcp}...");
            await FaultInjection
                .ActivateFaultInjectionAsync(
                    new AmqpTransportSettings(),
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
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
                if (connectionStatusChanges.ContainsKey(ConnectionStatus.Disconnected))
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
            sw.Reset();

            lastConnectionStatus.Should().Be(ConnectionStatus.Disconnected, $"Expected device to be {ConnectionStatus.Disconnected} but was {lastConnectionStatus}.");
            lastConnectionStatusChangeReason.Should().Be(ConnectionStatusChangeReason.Retry_Expired, $"Expected device to be {ConnectionStatusChangeReason.Retry_Expired} but was {lastConnectionStatusChangeReason}.");
            connectionStatusChanges.Should().NotContainKey(ConnectionStatus.Disconnected_Retrying, $"Shouldn't get {ConnectionStatus.Disconnected_Retrying} status change.");
            int connected = connectionStatusChanges[ConnectionStatus.Connected];
            connected.Should().Be(1, $"Should get {ConnectionStatus.Connected} once but got it {connected} times.");
            int disconnected = connectionStatusChanges[ConnectionStatus.Disconnected];
            disconnected.Should().Be(1, $"Should get {ConnectionStatus.Disconnected} once but got it {disconnected} times.");
        }

        [LoggedTestMethod]
        public async Task DuplicateDevice_NoRetry_NoPingpong_OpenAsync()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);

            Logger.Trace($"{nameof(DuplicateDevice_NoRetry_NoPingpong_OpenAsync)}: 2 device client instances with the same deviceId={testDevice.Id}.");

            var options = new IotHubClientOptions(new AmqpTransportSettings());
            using IotHubDeviceClient deviceClient1 = testDevice.CreateDeviceClient(options);
            using IotHubDeviceClient deviceClient2 = testDevice.CreateDeviceClient(options);

            Logger.Trace($"{nameof(DuplicateDevice_NoRetry_NoPingpong_OpenAsync)}: set device client instance 1 to no retry.");
            deviceClient1.SetRetryPolicy(new NoRetry());

            ConnectionStatus? lastConnectionStatusDevice1 = null;
            var connectionStatusChangesDevice1 = new Dictionary<ConnectionStatus, int>();
            deviceClient1.SetConnectionStatusChangesHandler((status, reason) =>
            {
                connectionStatusChangesDevice1.TryGetValue(status, out int count);
                count++;
                connectionStatusChangesDevice1[status] = count;
                lastConnectionStatusDevice1 = status;
            });

            ConnectionStatus? lastConnectionStatusDevice2 = null;
            var connectionStatusChangesDevice2 = new Dictionary<ConnectionStatus, int>();
            deviceClient2.SetConnectionStatusChangesHandler((status, reason) =>
            {
                connectionStatusChangesDevice2.TryGetValue(status, out int count);
                count++;
                connectionStatusChangesDevice2[status] = count;
                lastConnectionStatusDevice2 = status;
            });

            Logger.Trace($"{nameof(DuplicateDevice_NoRetry_NoPingpong_OpenAsync)}: device client instance 1 calling OpenAsync...");
            await deviceClient1.OpenAsync().ConfigureAwait(false);
            await deviceClient1
                .SetMethodHandlerAsync(
                    "empty_method",
                    (methodRequest, userContext) => Task.FromResult(new MethodResponse(200)),
                    deviceClient1)
                .ConfigureAwait(false);

            Logger.Trace($"{nameof(DuplicateDevice_NoRetry_NoPingpong_OpenAsync)}: device client instance 2 calling OpenAsync...");
            await deviceClient2.OpenAsync().ConfigureAwait(false);
            await deviceClient2
                .SetMethodHandlerAsync(
                    "empty_method",
                    (methodRequest, userContext) => Task.FromResult(new MethodResponse(200)),
                    deviceClient2)
                .ConfigureAwait(false);

            Logger.Trace($"{nameof(DuplicateDevice_NoRetry_NoPingpong_OpenAsync)}: waiting device client instance 1 to be kicked off...");
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < FaultInjection.LatencyTimeBuffer)
            {
                if (connectionStatusChangesDevice1.ContainsKey(ConnectionStatus.Disconnected))
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
            sw.Reset();

            lastConnectionStatusDevice1.Should().Be(ConnectionStatus.Disconnected, $"Excpected device 1 to be {ConnectionStatus.Disconnected} but was {lastConnectionStatusDevice1}.");
            connectionStatusChangesDevice1.Should().NotContainKey(ConnectionStatus.Disconnected_Retrying, $"Shouldn't get {ConnectionStatus.Disconnected_Retrying} status change.");
            int connected = connectionStatusChangesDevice1[ConnectionStatus.Connected];
            connected.Should().Be(1, $"Should get {ConnectionStatus.Connected} once but got it {connected} times.");
            int disconnected = connectionStatusChangesDevice1[ConnectionStatus.Disconnected];
            disconnected.Should().Be(1, $"Should get {ConnectionStatus.Disconnected} once but got it {disconnected} times.");

            lastConnectionStatusDevice2.Should().Be(ConnectionStatus.Connected, $"Expected device 2 to be {ConnectionStatus.Connected} but was {lastConnectionStatusDevice2}.");
        }
    }
}
