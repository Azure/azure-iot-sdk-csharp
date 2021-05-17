// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Iothub
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class AuthenticationWithTokenRefreshDisposalTests : E2EMsTestBase
    {
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

            // Perform the same steps again, reusing the previously created authentication method instance.
            DeviceClient deviceClient2 = DeviceClient.Create(testDevice.IoTHubHostName, authenticationMethod, transport);
            await deviceClient2.SendEventAsync(new Client.Message()).ConfigureAwait(false);
            await deviceClient2.CloseAsync();
            deviceClient2.Dispose();
            Logger.Trace("Test with instance 2 completed");
        }

        private async Task ReuseAuthenticationMethod_MuxedDevices(Client.TransportType transport, int devicesCount)
        {
            IList<TestDevice> testDevices = new List<TestDevice>();
            IList<AuthenticationWithTokenRefresh> authenticationMethods = new List<AuthenticationWithTokenRefresh>();

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

            // Create an instance of the device client, send a test message and then close and dispose it.
            for (int i = 0; i < devicesCount; i++)
            {
                DeviceClient deviceClient = DeviceClient.Create(testDevices[i].IoTHubHostName, authenticationMethods[i], new ITransportSettings[] { amqpTransportSettings });
                await deviceClient.SendEventAsync(new Client.Message()).ConfigureAwait(false);
                await deviceClient.CloseAsync();
                deviceClient.Dispose();
                Logger.Trace($"Test with client {i} completed.");
            }
            Logger.Trace($"Test run with instance 1 completed.");

            // Perform the same steps again, reusing the previously created authentication method instance.
            for (int i = 0; i < devicesCount; i++)
            {
                DeviceClient deviceClient = DeviceClient.Create(testDevices[i].IoTHubHostName, authenticationMethods[i], new ITransportSettings[] { amqpTransportSettings });
                await deviceClient.SendEventAsync(new Client.Message()).ConfigureAwait(false);
                await deviceClient.CloseAsync();
                deviceClient.Dispose();
                Logger.Trace($"Test with client {i} completed.");
            }
            Logger.Trace($"Test run with instance 2 completed.");
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
