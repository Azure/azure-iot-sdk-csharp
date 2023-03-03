// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("FaultInjection")]
        public async Task FaultInjection_NoRetry_NoRecovery_OpenAsync()
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);
            
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings())
            {
                RetryPolicy = new IotHubClientNoRetry(),
            };
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);

            VerboseTestLogger.WriteLine($"{nameof(FaultInjection_NoRetry_NoRecovery_OpenAsync)}: deviceId={testDevice.Id}");

            var connectionStatusChange = new Dictionary<ConnectionStatus, int>();

            void ConnectionStatusChangeHandler(ConnectionStatusInfo connectionStatusInfo)
            {
                connectionStatusChange.TryGetValue(connectionStatusInfo.Status, out int count);
                count++;
                connectionStatusChange[connectionStatusInfo.Status] = count;
            }
            deviceClient.ConnectionStatusChangeCallback = ConnectionStatusChangeHandler;

            VerboseTestLogger.WriteLine($"{nameof(FaultInjection_NoRetry_NoRecovery_OpenAsync)}: calling OpenAsync...");
            await testDevice.OpenWithRetryAsync().ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{nameof(FaultInjection_NoRetry_NoRecovery_OpenAsync)}: injecting fault {FaultInjectionConstants.FaultType_Tcp}...");
            await FaultInjection
                .ActivateFaultInjectionAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    deviceClient)
                .ConfigureAwait(false);

            await Task.Delay(FaultInjection.DefaultFaultDelay).ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{nameof(FaultInjection_NoRetry_NoRecovery_OpenAsync)}: waiting fault injection occurs...");
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < FaultInjection.DefaultFaultDuration)
            {
                if (connectionStatusChange.ContainsKey(ConnectionStatus.Disconnected))
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
            sw.Reset();

            ConnectionStatus lastConnectionStatus = deviceClient.ConnectionStatusInfo.Status;
            ConnectionStatusChangeReason lastConnectionStatusChangeReason = deviceClient.ConnectionStatusInfo.ChangeReason;

            lastConnectionStatus.Should().Be(ConnectionStatus.Disconnected, $"Expected device to be {ConnectionStatus.Disconnected} but was {lastConnectionStatus}.");
            lastConnectionStatusChangeReason.Should().Be(ConnectionStatusChangeReason.RetryExpired, $"Expected device to be {ConnectionStatusChangeReason.RetryExpired} but was {lastConnectionStatusChangeReason}.");
            connectionStatusChange.Should().NotContainKey(ConnectionStatus.DisconnectedRetrying, $"Shouldn't get {ConnectionStatus.DisconnectedRetrying} status change.");
            int connected = connectionStatusChange[ConnectionStatus.Connected];
            connected.Should().Be(1, $"Should get {ConnectionStatus.Connected} once but got it {connected} times.");
            int disconnected = connectionStatusChange[ConnectionStatus.Disconnected];
            disconnected.Should().Be(1, $"Should get {ConnectionStatus.Disconnected} once but got it {disconnected} times.");
        }
    }
}
