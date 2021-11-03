// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class MqttTransportHandlerShutdownTests : E2EMsTestBase
    {
        private static readonly string _devicePrefix = $"E2E_{nameof(MqttTransportHandlerShutdownTests)}_";

        [LoggedTestMethod]
        public async Task MqttTransport_Tcp_LongTimeout_OnCloseAsync()
        {
            //arrange
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);
            var transportSettings = new MqttTransportSettings(Client.TransportType.Mqtt_Tcp_Only);
            transportSettings.GracefulEventLoopShutdownTimeout = TimeSpan.FromSeconds(30);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(new ITransportSettings[] { transportSettings });

            Logger.Trace($"{nameof(MqttTransport_Tcp_LongTimeout_OnCloseAsync)}: deviceId={testDevice.Id}");
            Logger.Trace($"{nameof(MqttTransport_Tcp_LongTimeout_OnCloseAsync)}: calling OpenAsync...");
            await deviceClient.OpenAsync().ConfigureAwait(false);
            Logger.Trace($"{nameof(MqttTransport_Tcp_LongTimeout_OnCloseAsync)}: calling CloseAsync...");

            //act
            var sw = Stopwatch.StartNew();
            await deviceClient.CloseAsync();
            sw.Stop();

            //assert
            sw.Elapsed.Should().BeCloseTo(transportSettings.GracefulEventLoopShutdownTimeout, 1000);
        }

        [LoggedTestMethod]
        public async Task MqttTransport_Ws_LongTimeout_OnCloseAsync()
        {
            //arrange
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);
            var transportSettings = new MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only);
            transportSettings.GracefulEventLoopShutdownTimeout = TimeSpan.FromSeconds(30);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(new ITransportSettings[] { transportSettings });


            Logger.Trace($"{nameof(MqttTransport_Ws_LongTimeout_OnCloseAsync)}: deviceId={testDevice.Id}");
            Logger.Trace($"{nameof(MqttTransport_Ws_LongTimeout_OnCloseAsync)}: calling OpenAsync...");
            await deviceClient.OpenAsync().ConfigureAwait(false);
            Logger.Trace($"{nameof(MqttTransport_Ws_LongTimeout_OnCloseAsync)}: calling CloseAsync...");

            //act
            var sw = Stopwatch.StartNew();
            await deviceClient.CloseAsync();
            sw.Stop();

            //assert
            sw.Elapsed.Should().BeCloseTo(transportSettings.GracefulEventLoopShutdownTimeout, 1000);
        }

        [LoggedTestMethod]
        public async Task MqttTransport_Tcp_ShortTimeout_OnCloseAsync()
        {
            //arrange
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);
            var transportSettings = new MqttTransportSettings(Client.TransportType.Mqtt_Tcp_Only);
            transportSettings.GracefulEventLoopShutdownTimeout = TimeSpan.FromMilliseconds(250);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(new ITransportSettings[] { transportSettings });

            Logger.Trace($"{nameof(MqttTransport_Tcp_ShortTimeout_OnCloseAsync)}: deviceId={testDevice.Id}");
            Logger.Trace($"{nameof(MqttTransport_Tcp_ShortTimeout_OnCloseAsync)}: calling OpenAsync...");
            await deviceClient.OpenAsync().ConfigureAwait(false);
            Logger.Trace($"{nameof(MqttTransport_Tcp_ShortTimeout_OnCloseAsync)}: calling CloseAsync...");

            //act
            var sw = Stopwatch.StartNew();
            await deviceClient.CloseAsync();
            sw.Stop();

            //assert
            sw.Elapsed.Should().BeCloseTo(transportSettings.GracefulEventLoopShutdownTimeout, 150);
        }

        [LoggedTestMethod]
        public async Task MqttTransport_Ws_ShortTimeout_OnCloseAsync()
        {

            //arrange
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);
            var transportSettings = new MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only);
            transportSettings.GracefulEventLoopShutdownTimeout = TimeSpan.FromMilliseconds(250);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(new ITransportSettings[] { transportSettings });

            Logger.Trace($"{nameof(MqttTransport_Ws_ShortTimeout_OnCloseAsync)}: deviceId={testDevice.Id}");
            Logger.Trace($"{nameof(MqttTransport_Ws_ShortTimeout_OnCloseAsync)}: calling OpenAsync...");
            await deviceClient.OpenAsync().ConfigureAwait(false);
            Logger.Trace($"{nameof(MqttTransport_Ws_ShortTimeout_OnCloseAsync)}: calling CloseAsync...");

            //act
            var sw = Stopwatch.StartNew();
            await deviceClient.CloseAsync();
            sw.Stop();

            //assert
            sw.Elapsed.Should().BeCloseTo(transportSettings.GracefulEventLoopShutdownTimeout, 150);
        }

        [LoggedTestMethod]
        public async Task MqttTransport_Tcp_DefaultTimeout_OnCloseAsync()
        {
            //arrange
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Mqtt_Tcp_Only);

            Logger.Trace($"{nameof(MqttTransport_Tcp_DefaultTimeout_OnCloseAsync)}: deviceId={testDevice.Id}");
            Logger.Trace($"{nameof(MqttTransport_Tcp_DefaultTimeout_OnCloseAsync)}: calling OpenAsync...");
            await deviceClient.OpenAsync().ConfigureAwait(false);
            Logger.Trace($"{nameof(MqttTransport_Tcp_DefaultTimeout_OnCloseAsync)}: calling CloseAsync...");
            
            //act
            var sw = Stopwatch.StartNew();
            await deviceClient.CloseAsync();
            sw.Stop();

            //assert
            sw.Elapsed.Should().BeCloseTo(TimeSpan.FromSeconds(1), 200);
        }

        [LoggedTestMethod]
        public async Task MqttTransport_Ws_DefaultTimeout_OnCloseAsync()
        {
            //arrange
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Mqtt_WebSocket_Only);

            Logger.Trace($"{nameof(MqttTransport_Tcp_DefaultTimeout_OnCloseAsync)}: deviceId={testDevice.Id}");
            Logger.Trace($"{nameof(MqttTransport_Tcp_DefaultTimeout_OnCloseAsync)}: calling OpenAsync...");
            await deviceClient.OpenAsync().ConfigureAwait(false);
            Logger.Trace($"{nameof(MqttTransport_Tcp_DefaultTimeout_OnCloseAsync)}: calling CloseAsync...");


            //act
            var sw = Stopwatch.StartNew();
            await deviceClient.CloseAsync();
            sw.Stop();

            //assert
            sw.Elapsed.Should().BeCloseTo(TimeSpan.FromSeconds(1), 200);
        }
    }
}
