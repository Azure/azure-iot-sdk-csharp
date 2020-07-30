// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private readonly string DevicePrefix = $"E2E_{nameof(NoRetryE2ETests)}_";

        [LoggedTestMethod]
        [TestCategory("FaultInjection")]
        public async Task FaultInjection_NoRecovery()
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Amqp_Tcp_Only);

            Logger.Trace($"{nameof(FaultInjection_NoRecovery)}: deviceId={testDevice.Id}");
            deviceClient.SetRetryPolicy(new NoRetry());

            ConnectionStatus? lastConnectionStatus = null;
            Dictionary<ConnectionStatus, int> connectionStatusChanges = new Dictionary<ConnectionStatus, int>();
            deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                connectionStatusChanges.TryGetValue(status, out int count);
                count++;
                connectionStatusChanges[status] = count;
                lastConnectionStatus = status;
            });

            Logger.Trace($"{nameof(FaultInjection_NoRecovery)}: calling OpenAsync...");
            await deviceClient.OpenAsync().ConfigureAwait(false);

            Logger.Trace($"{nameof(FaultInjection_NoRecovery)}: injecting fault {FaultInjection.FaultType_Tcp}...");
            await FaultInjection
                .ActivateFaultInjectionAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    deviceClient,
                    Logger)
                .ConfigureAwait(false);

            await Task.Delay(FaultInjection.DefaultDelayInSec).ConfigureAwait(false);

            Logger.Trace($"{nameof(FaultInjection_NoRecovery)}: waiting fault injection occurs...");
            for (int i = 0; i < FaultInjection.LatencyTimeBufferInSec; i++)
            {
                if (connectionStatusChanges.ContainsKey(ConnectionStatus.Disconnected))
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }

            Assert.AreEqual(ConnectionStatus.Disconnected, lastConnectionStatus, $"Excepeted device to be {ConnectionStatus.Disconnected} but was {lastConnectionStatus}.");
            Assert.IsFalse(connectionStatusChanges.ContainsKey(ConnectionStatus.Disconnected_Retrying), $"Shouldn't get {ConnectionStatus.Disconnected_Retrying} status change.");
            int connected = connectionStatusChanges[ConnectionStatus.Connected];
            Assert.AreEqual(1, connected, $"Should get {ConnectionStatus.Connected} once but was {connected}.");
            int disconnected = connectionStatusChanges[ConnectionStatus.Disconnected];
            Assert.AreEqual(1, disconnected, $"Should get {ConnectionStatus.Disconnected} once but was {disconnected}.");
        }

        [LoggedTestMethod]
        public async Task Duplicated_NoPingpong()
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);

            Logger.Trace($"{nameof(Duplicated_NoPingpong)}: 2 device client instances with the same deviceId={testDevice.Id}.");

            using DeviceClient deviceClient1 = testDevice.CreateDeviceClient(Client.TransportType.Amqp_Tcp_Only);
            using DeviceClient deviceClient2 = testDevice.CreateDeviceClient(Client.TransportType.Amqp_Tcp_Only);

            Logger.Trace($"{nameof(Duplicated_NoPingpong)}: set device client instance 1 to no retry.");
            deviceClient1.SetRetryPolicy(new NoRetry());

            ConnectionStatus? lastConnectionStatus = null;
            var connectionStatusChanges = new Dictionary<ConnectionStatus, int>();
            deviceClient1.SetConnectionStatusChangesHandler((status, reason) =>
            {
                connectionStatusChanges.TryGetValue(status, out int count);
                count++;
                connectionStatusChanges[status] = count;
                lastConnectionStatus = status;
            });

            Logger.Trace($"{nameof(FaultInjection_NoRecovery)}: device client instance 1 calling OpenAsync...");
            await deviceClient1.OpenAsync().ConfigureAwait(false);
            await deviceClient1
                .SetMethodHandlerAsync(
                    "dummy_method",
                    (methodRequest, userContext) => Task.FromResult(new MethodResponse(200)),
                    deviceClient1)
                .ConfigureAwait(false);

            Logger.Trace($"{nameof(FaultInjection_NoRecovery)}: device client instance 2 calling OpenAsync...");
            await deviceClient2.OpenAsync().ConfigureAwait(false);
            await deviceClient2
                .SetMethodHandlerAsync(
                    "dummy_method",
                    (methodRequest, userContext) => Task.FromResult(new MethodResponse(200)),
                    deviceClient2)
                .ConfigureAwait(false);

            Logger.Trace($"{nameof(Duplicated_NoPingpong)}: waiting device client instance 1 to be kicked off...");
            for (int i = 0; i < FaultInjection.LatencyTimeBufferInSec; i++)
            {
                if (connectionStatusChanges.ContainsKey(ConnectionStatus.Disconnected))
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }

            Assert.AreEqual(ConnectionStatus.Disconnected, lastConnectionStatus, $"Excepeted device to be {ConnectionStatus.Disconnected} but was {lastConnectionStatus}.");
            Assert.IsFalse(connectionStatusChanges.ContainsKey(ConnectionStatus.Disconnected_Retrying), $"Shouldn't get {ConnectionStatus.Disconnected_Retrying} status change.");
            int connected = connectionStatusChanges[ConnectionStatus.Connected];
            Assert.AreEqual(1, connected, $"Should get {ConnectionStatus.Connected} once but was {connected}.");
            int disconnected = connectionStatusChanges[ConnectionStatus.Disconnected];
            Assert.AreEqual(1, disconnected, $"Should get {ConnectionStatus.Disconnected} once but was {disconnected}.");
        }
    }
}
