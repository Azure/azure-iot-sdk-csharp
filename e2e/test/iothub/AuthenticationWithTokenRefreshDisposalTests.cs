﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
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
    public class AuthenticationWithTokenRefreshDisposalTests : E2EMsTestBase
    {
        public static readonly TimeSpan MaxWaitTime = TimeSpan.FromSeconds(10);
        private readonly string _devicePrefix = $"E2E_{nameof(AuthenticationWithTokenRefreshDisposalTests)}_";

        [LoggedTestMethod]
        public async Task DeviceSak_ReusableAuthenticationMethod_SingleDevicePerConnection_Amqp()
        {
            await ReuseAuthenticationMethod_SingleDevice(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceSak_ReusableAuthenticationMethod_SingleDevicePerConnection_AmqpWs()
        {
            await ReuseAuthenticationMethod_SingleDevice(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceSak_ReusableAuthenticationMethod_SingleDevicePerConnection_Mqtt()
        {
            await ReuseAuthenticationMethod_SingleDevice(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceSak_ReusableAuthenticationMethod_SingleDevicePerConnection_MqttWs()
        {
            await ReuseAuthenticationMethod_SingleDevice(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceSak_ReusableAuthenticationMethod_SingleDevicePerConnection_Http()
        {
            await ReuseAuthenticationMethod_SingleDevice(Client.TransportType.Http1).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceSak_ReusableAuthenticationMethod_MuxedDevicesPerConnection_Amqp()
        {
            await ReuseAuthenticationMethod_MuxedDevices(Client.TransportType.Amqp_Tcp_Only, 2);
        }

        [LoggedTestMethod]
        public async Task DeviceSak_ReusableAuthenticationMethod_MuxedDevicesPerConnection_AmqpWs()
        {
            await ReuseAuthenticationMethod_MuxedDevices(Client.TransportType.Amqp_WebSocket_Only, 2);
        }

        private async Task ReuseAuthenticationMethod_SingleDevice(Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var authenticationMethod = new DeviceAuthenticationSasToken(testDevice.ConnectionString);

            // Create an instance of the device client, send a test message and then close and dispose it.
            DeviceClient deviceClient = DeviceClient.Create(testDevice.IoTHubHostName, authenticationMethod, transport);
            await deviceClient.SendEventAsync(new Client.Message()).ConfigureAwait(false);
            await deviceClient.CloseAsync();
            deviceClient.Dispose();
            Logger.Trace("Test with instance 1 completed");

            // Perform the same steps again, reusing the previously created authentication method instance to ensure
            // that the sdk did not dispose the user supplied authentication method instance.
            DeviceClient deviceClient2 = DeviceClient.Create(testDevice.IoTHubHostName, authenticationMethod, transport);
            await deviceClient2.SendEventAsync(new Client.Message()).ConfigureAwait(false);
            await deviceClient2.CloseAsync();
            deviceClient2.Dispose();
            Logger.Trace("Test with instance 2 completed, reused the previously created authentication method instance for the device client.");

            authenticationMethod.Dispose();
        }

        private async Task ReuseAuthenticationMethod_MuxedDevices(Client.TransportType transport, int devicesCount)
        {
            IList<TestDevice> testDevices = new List<TestDevice>();
            IList<DeviceClient> deviceClients = new List<DeviceClient>();
            IList<AuthenticationWithTokenRefresh> authenticationMethods = new List<AuthenticationWithTokenRefresh>();
            IList<AmqpConnectionStatusChange> amqpConnectionStatuses = new List<AmqpConnectionStatusChange>();

            // Set up amqp transport settings to multiplex all device sessions over the same amqp connection.
            var amqpTransportSettings = new AmqpTransportSettings(transport)
            {
                AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings
                {
                    Pooling = true,
                    MaxPoolSize = 1,
                },
            };

            for (int i = 0; i < devicesCount; i++)
            {
                TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
                var authenticationMethod = new DeviceAuthenticationSasToken(testDevice.ConnectionString);

                testDevices.Add(testDevice);
                authenticationMethods.Add(authenticationMethod);
            }

            // Initialize the client instances, set the connection status change handler and open the connection.
            for (int i = 0; i < devicesCount; i++)
            {
                DeviceClient deviceClient = DeviceClient.Create(testDevices[i].IoTHubHostName, authenticationMethods[i], new ITransportSettings[] { amqpTransportSettings });

                var amqpConnectionStatusChange = new AmqpConnectionStatusChange(testDevices[i].Id, Logger);
                deviceClient.SetConnectionStatusChangesHandler(amqpConnectionStatusChange.ConnectionStatusChangesHandler);
                amqpConnectionStatuses.Add(amqpConnectionStatusChange);

                await deviceClient.OpenAsync().ConfigureAwait(false);
                deviceClients.Add(deviceClient);
            }

            // Close and dispose client instance 1.
            // The closed client should report a status of "disabled" while the rest of them should be connected.
            // This is to ensure that disposal on one multiplexed device doesn't cause cascading failures
            // in the rest of the devices on the same tcp connection.

            await deviceClients[0].CloseAsync().ConfigureAwait(false);
            deviceClients[0].Dispose();

            amqpConnectionStatuses[0].LastConnectionStatus.Should().Be(ConnectionStatus.Disabled);

            Logger.Trace($"{nameof(ReuseAuthenticationMethod_MuxedDevices)}: Confirming the rest of the multiplexed devices are online and operational.");

            bool notRecovered = true;
            var sw = Stopwatch.StartNew();
            while (notRecovered && sw.Elapsed < MaxWaitTime)
            {
                notRecovered = false;
                for (int i = 1; i < devicesCount; i++)
                {
                    if (amqpConnectionStatuses[i].LastConnectionStatus != ConnectionStatus.Connected)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        notRecovered = true;
                        break;
                    }
                }
            }

            notRecovered.Should().BeFalse();

            // Send a message through the rest of the multiplexed client instances.
            for (int i = 1; i < devicesCount; i++)
            {
                await deviceClients[i].SendEventAsync(new Client.Message()).ConfigureAwait(false);
                Logger.Trace($"Test with client {i} completed.");
            }

            // Close and dispose all of the client instances.
            for (int i = 1; i < devicesCount; i++)
            {
                await deviceClients[i].CloseAsync().ConfigureAwait(false);
                deviceClients[i].Dispose();
            }

            deviceClients.Clear();
            amqpConnectionStatuses.Clear();

            // Initialize the client instances by reusing the created authentication methods and open the connection.
            for (int i = 0; i < devicesCount; i++)
            {
                DeviceClient deviceClient = DeviceClient.Create(testDevices[i].IoTHubHostName, authenticationMethods[i], new ITransportSettings[] { amqpTransportSettings });

                var amqpConnectionStatusChange = new AmqpConnectionStatusChange(testDevices[i].Id, Logger);
                deviceClient.SetConnectionStatusChangesHandler(amqpConnectionStatusChange.ConnectionStatusChangesHandler);
                amqpConnectionStatuses.Add(amqpConnectionStatusChange);

                await deviceClient.OpenAsync().ConfigureAwait(false);
                deviceClients.Add(deviceClient);
            }

            // Ensure that all clients are connected successfully, and the close and dispose the instances.
            for (int i = 0; i < devicesCount; i++)
            {
                amqpConnectionStatuses[i].LastConnectionStatus.Should().Be(ConnectionStatus.Connected);

                await deviceClients[i].CloseAsync();
                deviceClients[i].Dispose();

                amqpConnectionStatuses[i].LastConnectionStatus.Should().Be(ConnectionStatus.Disabled);
            }
        }

        private class DeviceAuthenticationSasToken : DeviceAuthenticationWithTokenRefresh
        {
            private const string SasTokenTargetFormat = "{0}/devices/{1}";
            private readonly IotHubConnectionStringBuilder _connectionStringBuilder;

            public DeviceAuthenticationSasToken(
                string connectionString)
                : base(GetDeviceIdFromConnectionString(connectionString))
            {
                if (connectionString == null)
                {
                    throw new ArgumentNullException(nameof(connectionString));
                }

                _connectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
            }

            protected override Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLive)
            {
                var builder = new SharedAccessSignatureBuilder
                {
                    Key = _connectionStringBuilder.SharedAccessKey,
                    TimeToLive = TimeSpan.FromSeconds(suggestedTimeToLive),
                };

                if (_connectionStringBuilder.SharedAccessKeyName == null)
                {
                    builder.Target = string.Format(
                        CultureInfo.InvariantCulture,
                        SasTokenTargetFormat,
                        iotHub,
                        WebUtility.UrlEncode(DeviceId));
                }
                else
                {
                    builder.KeyName = _connectionStringBuilder.SharedAccessKeyName;
                    builder.Target = _connectionStringBuilder.HostName;
                }

                return Task.FromResult(builder.ToSignature());
            }

            private static string GetDeviceIdFromConnectionString(string connectionString)
            {
                if (connectionString == null)
                {
                    throw new ArgumentNullException(nameof(connectionString));
                }

                var builder = IotHubConnectionStringBuilder.Create(connectionString);
                return builder.DeviceId;
            }
        }
    }
}
