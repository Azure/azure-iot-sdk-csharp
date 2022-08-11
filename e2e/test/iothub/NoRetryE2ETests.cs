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

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [TestCategory("FaultInjection")]
        public async Task FaultInjection_NoRetry_NoRecovery_OpenAsync()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientAmqpSettings()));

            Logger.Trace($"{nameof(FaultInjection_NoRetry_NoRecovery_OpenAsync)}: deviceId={testDevice.Id}");
            deviceClient.SetRetryPolicy(new NoRetry());

            var connectionStateChange = new Dictionary<ConnectionState, int>();
            deviceClient.SetConnectionStateChangeHandler((state, reason) =>
            {
                connectionStateChange.TryGetValue(state, out int count);
                count++;
                connectionStateChange[state] = count;
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
                if (connectionStateChange.ContainsKey(ConnectionState.Disconnected))
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
            sw.Reset();

            var lastConnectionState = deviceClient.ConnectionInfo.State;
            var lastConnectionStateChangeReason = deviceClient.ConnectionInfo.ChangeReason;

            lastConnectionState.Should().Be(ConnectionState.Disconnected, $"Expected device to be {ConnectionState.Disconnected} but was {lastConnectionState}.");
            lastConnectionStateChangeReason.Should().Be(ConnectionStateChangeReason.RetryExpired, $"Expected device to be {ConnectionStateChangeReason.RetryExpired} but was {lastConnectionStateChangeReason}.");
            connectionStateChange.Should().NotContainKey(ConnectionState.DisconnectedRetrying, $"Shouldn't get {ConnectionState.DisconnectedRetrying} state change.");
            int connected = connectionStateChange[ConnectionState.Connected];
            connected.Should().Be(1, $"Should get {ConnectionState.Connected} once but got it {connected} times.");
            int disconnected = connectionStateChange[ConnectionState.Disconnected];
            disconnected.Should().Be(1, $"Should get {ConnectionState.Disconnected} once but got it {disconnected} times.");
        }

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

            var connectionStateChangeDevice1 = new Dictionary<ConnectionState, int>();
            deviceClient1.SetConnectionStateChangeHandler((state, reason) =>
            {
                connectionStateChangeDevice1.TryGetValue(state, out int count);
                count++;
                connectionStateChangeDevice1[state] = count;
            });

            var connectionStateChangeDevice2 = new Dictionary<ConnectionState, int>();
            deviceClient2.SetConnectionStateChangeHandler((state, reason) =>
            {
                connectionStateChangeDevice2.TryGetValue(state, out int count);
                count++;
                connectionStateChangeDevice2[state] = count;
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
                if (connectionStateChangeDevice1.ContainsKey(ConnectionState.Disconnected))
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
            sw.Reset();

            var lastConnectionStateDevice1 = deviceClient1.ConnectionInfo.State;
            lastConnectionStateDevice1.Should().Be(ConnectionState.Disconnected, $"Excpected device 1 to be {ConnectionState.Disconnected} but was {lastConnectionStateDevice1}.");
            connectionStateChangeDevice1.Should().NotContainKey(ConnectionState.DisconnectedRetrying, $"Shouldn't get {ConnectionState.DisconnectedRetrying} state change.");
            int connected = connectionStateChangeDevice1[ConnectionState.Connected];
            connected.Should().Be(1, $"Should get {ConnectionState.Connected} once but got it {connected} times.");
            int disconnected = connectionStateChangeDevice1[ConnectionState.Disconnected];
            disconnected.Should().Be(1, $"Should get {ConnectionState.Disconnected} once but got it {disconnected} times.");

            var lastConnectionStateDevice2 = deviceClient2.ConnectionInfo.State;
            lastConnectionStateDevice2.Should().Be(ConnectionState.Connected, $"Expected device 2 to be {ConnectionState.Connected} but was {lastConnectionStateDevice2}.");
        }
    }
}
