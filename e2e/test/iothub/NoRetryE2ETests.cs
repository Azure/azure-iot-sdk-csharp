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
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientAmqpSettings()));

            Logger.Trace($"{nameof(FaultInjection_NoRetry_NoRecovery_OpenAsync)}: deviceId={testDevice.Id}");
            deviceClient.SetRetryPolicy(new NoRetry());

            ConnectionState? lastConnectionStatus = null;
            ConnectionStateChangesReason? lastConnectionStatusChangeReason = null;
            var connectionStatusChanges = new Dictionary<ConnectionState, int>();
            deviceClient.SetConnectionStateChangesHandler((status, reason) =>
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
                    new IotHubClientAmqpSettings(),
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
                if (connectionStatusChanges.ContainsKey(ConnectionState.Disconnected))
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
            sw.Reset();

            lastConnectionStatus.Should().Be(ConnectionState.Disconnected, $"Expected device to be {ConnectionState.Disconnected} but was {lastConnectionStatus}.");
            lastConnectionStatusChangeReason.Should().Be(ConnectionStateChangesReason.RetryExpired, $"Expected device to be {ConnectionStateChangesReason.RetryExpired} but was {lastConnectionStatusChangeReason}.");
            connectionStatusChanges.Should().NotContainKey(ConnectionState.DisconnectedRetrying, $"Shouldn't get {ConnectionState.DisconnectedRetrying} status change.");
            int connected = connectionStatusChanges[ConnectionState.Connected];
            connected.Should().Be(1, $"Should get {ConnectionState.Connected} once but got it {connected} times.");
            int disconnected = connectionStatusChanges[ConnectionState.Disconnected];
            disconnected.Should().Be(1, $"Should get {ConnectionState.Disconnected} once but got it {disconnected} times.");
        }

        [LoggedTestMethod]
        public async Task DuplicateDevice_NoRetry_NoPingpong_OpenAsync()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);

            Logger.Trace($"{nameof(DuplicateDevice_NoRetry_NoPingpong_OpenAsync)}: 2 device client instances with the same deviceId={testDevice.Id}.");

            var options = new IotHubClientOptions(new IotHubClientAmqpSettings());
            using IotHubDeviceClient deviceClient1 = testDevice.CreateDeviceClient(options);
            using IotHubDeviceClient deviceClient2 = testDevice.CreateDeviceClient(options);

            Logger.Trace($"{nameof(DuplicateDevice_NoRetry_NoPingpong_OpenAsync)}: set device client instance 1 to no retry.");
            deviceClient1.SetRetryPolicy(new NoRetry());

            ConnectionState? lastConnectionStatusDevice1 = null;
            var connectionStatusChangesDevice1 = new Dictionary<ConnectionState, int>();
            deviceClient1.SetConnectionStateChangesHandler((status, reason) =>
            {
                connectionStatusChangesDevice1.TryGetValue(status, out int count);
                count++;
                connectionStatusChangesDevice1[status] = count;
                lastConnectionStatusDevice1 = status;
            });

            ConnectionState? lastConnectionStatusDevice2 = null;
            var connectionStatusChangesDevice2 = new Dictionary<ConnectionState, int>();
            deviceClient2.SetConnectionStateChangesHandler((status, reason) =>
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
                if (connectionStatusChangesDevice1.ContainsKey(ConnectionState.Disconnected))
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
            sw.Reset();

            lastConnectionStatusDevice1.Should().Be(ConnectionState.Disconnected, $"Excpected device 1 to be {ConnectionState.Disconnected} but was {lastConnectionStatusDevice1}.");
            connectionStatusChangesDevice1.Should().NotContainKey(ConnectionState.DisconnectedRetrying, $"Shouldn't get {ConnectionState.DisconnectedRetrying} status change.");
            int connected = connectionStatusChangesDevice1[ConnectionState.Connected];
            connected.Should().Be(1, $"Should get {ConnectionState.Connected} once but got it {connected} times.");
            int disconnected = connectionStatusChangesDevice1[ConnectionState.Disconnected];
            disconnected.Should().Be(1, $"Should get {ConnectionState.Disconnected} once but got it {disconnected} times.");

            lastConnectionStatusDevice2.Should().Be(ConnectionState.Connected, $"Expected device 2 to be {ConnectionState.Connected} but was {lastConnectionStatusDevice2}.");
        }
    }
}
