// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Properties
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("FaultInjection")]
    public class PropertiesFaultInjectionTests : E2EMsTestBase
    {
        private static readonly string s_devicePrefix = $"E2E_{nameof(PropertiesFaultInjectionTests)}_";

        [LoggedTestMethod]
        public async Task Properties_DeviceUpdateClientPropertiesTcpConnRecovery_Mqtt()
        {
            await Properties_DeviceUpdateClientPropertiesRecoveryAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_DeviceUpdateClientPropertiesTcpConnRecovery_MqttWs()
        {
            await Properties_DeviceUpdateClientPropertiesRecoveryAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_DeviceUpdateClientPropertiesGracefulShutdownRecovery_Mqtt()
        {
            await Properties_DeviceUpdateClientPropertiesRecoveryAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_DeviceUpdateClientPropertiesGracefulShutdownRecovery_MqttWs()
        {
            await Properties_DeviceUpdateClientPropertiesRecoveryAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_DeviceReceivePropertyUpdateTcpConnRecovery_Mqtt()
        {
            await Properties_DeviceReceivePropertyUpdateRecoveryAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_DeviceReceivePropertyUpdateTcpConnRecovery_MqttWs()
        {
            await Properties_DeviceReceivePropertyUpdateRecoveryAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_DeviceReceivePropertyUpdateGracefulShutdownRecovery_Mqtt()
        {
            await Properties_DeviceReceivePropertyUpdateRecoveryAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_DeviceReceivePropertyUpdateGracefulShutdownRecovery_MqttWs()
        {
            await Properties_DeviceReceivePropertyUpdateRecoveryAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        private async Task Properties_DeviceUpdateClientPropertiesRecoveryAsync(
            Client.TransportType transport,
            string faultType,
            string reason,
            TimeSpan delayInSec,
            string proxyAddress = null)
        {
            static async Task TestOperationAsync(DeviceClient deviceClient, TestDevice testDevice)
            {
                string propName = Guid.NewGuid().ToString();
                string propValue = Guid.NewGuid().ToString();

                var properties = new ClientPropertyCollection();
                properties.AddRootProperty(propName, propValue);

                await deviceClient.UpdateClientPropertiesAsync(properties).ConfigureAwait(false);

                ClientProperties clientProperties = await deviceClient.GetClientPropertiesAsync().ConfigureAwait(false);
                clientProperties.Should().NotBeNull();

                bool isPropertyPresent = clientProperties.TryGetValue(propName, out string propFromCollection);
                isPropertyPresent.Should().BeTrue();
                propFromCollection.Should().Be(propValue);
            }

            await FaultInjection
                .TestErrorInjectionAsync(
                    s_devicePrefix,
                    TestDeviceType.Sasl,
                    transport,
                    proxyAddress,
                    faultType,
                    reason,
                    delayInSec,
                    FaultInjection.DefaultFaultDuration,
                    (d, t) => { return Task.FromResult(false); },
                    TestOperationAsync,
                    () => { return Task.FromResult(false); },
                    Logger)
                .ConfigureAwait(false);
        }

        private async Task RegistryManagerUpdateDesiredPropertyAsync(string deviceId, string propName, string propValue)
        {
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;

            await registryManager.UpdateTwinAsync(deviceId, twinPatch, "*").ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task Properties_DeviceReceivePropertyUpdateRecoveryAsync(
            Client.TransportType transport,
            string faultType,
            string reason,
            TimeSpan delayInSec,
            string proxyAddress = null)
        {
            TestDeviceCallbackHandler testDeviceCallbackHandler = null;
            using var cts = new CancellationTokenSource(FaultInjection.RecoveryTime);

            string propName = Guid.NewGuid().ToString();

            // Configure the callback and start accepting property update notifications.
            async Task InitOperationAsync(DeviceClient deviceClient, TestDevice testDevice)
            {
                testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);
                await testDeviceCallbackHandler.SetClientPropertyUpdateCallbackHandlerAsync<string>(propName).ConfigureAwait(false);
            }

            // Change the properties from the service side and verify the device received it.
            async Task TestOperationAsync(DeviceClient deviceClient, TestDevice testDevice)
            {
                string propValue = Guid.NewGuid().ToString();
                testDeviceCallbackHandler.ExpectedClientPropertyValue = propValue;

                Logger.Trace($"{nameof(Properties_DeviceReceivePropertyUpdateRecoveryAsync)}: name={propName}, value={propValue}");

                await Task
                    .WhenAll(
                        RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue),
                        testDeviceCallbackHandler.WaitForClientPropertyUpdateCallbcakAsync(cts.Token))
                    .ConfigureAwait(false);
            }

            // Cleanup references.
            Task CleanupOperationAsync()
            {
                testDeviceCallbackHandler?.Dispose();
                return Task.FromResult(false);
            }

            await FaultInjection
                .TestErrorInjectionAsync(
                    s_devicePrefix,
                    TestDeviceType.Sasl,
                    transport,
                    proxyAddress,
                    faultType,
                    reason,
                    delayInSec,
                    FaultInjection.DefaultFaultDuration,
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupOperationAsync,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
