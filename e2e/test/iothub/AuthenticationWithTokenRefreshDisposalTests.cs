// Copyright (c) Microsoft. All rights reserved.
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
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class AuthenticationWithTokenRefreshDisposalTests : E2EMsTestBase
    {
        public static readonly TimeSpan MaxWaitTime = TimeSpan.FromSeconds(10);
        private readonly string _devicePrefix = $"{nameof(AuthenticationWithTokenRefreshDisposalTests)}_";

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceSak_ReusableAuthenticationMethod_SingleDevicePerConnection_Amqp()
        {
            await ReuseAuthenticationMethod_SingleDevice(new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceSak_ReusableAuthenticationMethod_SingleDevicePerConnection_AmqpWs()
        {
            await ReuseAuthenticationMethod_SingleDevice(new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceSak_ReusableAuthenticationMethod_SingleDevicePerConnection_Mqtt()
        {
            await ReuseAuthenticationMethod_SingleDevice(new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceSak_ReusableAuthenticationMethod_SingleDevicePerConnection_MqttWs()
        {
            await ReuseAuthenticationMethod_SingleDevice(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceSak_ReusableAuthenticationMethod_SingleDevicePerConnection_Http()
        {
            await ReuseAuthenticationMethod_SingleDevice(new IotHubClientHttpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceSak_ReusableAuthenticationMethod_MuxedDevicesPerConnection_Amqp()
        {
            await ReuseAuthenticationMethod_MuxedDevices(new IotHubClientAmqpSettings(), 2).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceSak_ReusableAuthenticationMethod_MuxedDevicesPerConnection_AmqpWs()
        {
            await ReuseAuthenticationMethod_MuxedDevices(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), 2).ConfigureAwait(false); ;
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_AuthenticationMethodDisposesTokenRefresher_Http()
        {
            await AuthenticationMethodDisposesTokenRefresher(new IotHubClientHttpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_AuthenticationMethodDisposesTokenRefresher_Amqp()
        {
            await AuthenticationMethodDisposesTokenRefresher(new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_AuthenticationMethodDisposesTokenRefresher_AmqpWs()
        {
            await AuthenticationMethodDisposesTokenRefresher(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket)).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_AuthenticationMethodDisposesTokenRefresher_Mqtt()
        {
            await AuthenticationMethodDisposesTokenRefresher(new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_AuthenticationMethodDisposesTokenRefresher_MqttWs()
        {
            await AuthenticationMethodDisposesTokenRefresher(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)).ConfigureAwait(false);
        }

        private async Task AuthenticationMethodDisposesTokenRefresher(IotHubClientTransportSettings transportSettings)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var authenticationMethod = new DeviceAuthenticationSasToken(testDevice.ConnectionString, disposeWithClient: true);

            // Create an instance of the device client, send a test message and then close and dispose it.
            var options = new IotHubClientOptions(transportSettings);
            var deviceClient = IotHubDeviceClient.Create(testDevice.IotHubHostName, authenticationMethod, options);
            using var message1 = new Client.Message();
            await deviceClient.SendEventAsync(message1).ConfigureAwait(false);
            await deviceClient.CloseAsync();
            deviceClient.Dispose();
            Logger.Trace("Test with instance 1 completed");

            // Perform the same steps again, reusing the previously created authentication method instance.
            // Since the default behavior is to dispose AuthenticationWithTokenRefresh authentication method on DeviceClient disposal,
            // this should now throw an ObjectDisposedException.
            var deviceClient2 = IotHubDeviceClient.Create(testDevice.IotHubHostName, authenticationMethod, options);
            using var message2 = new Client.Message();

            Func<Task> act = async () => await deviceClient2.SendEventAsync(message2).ConfigureAwait(false);
            await act.Should().ThrowAsync<ObjectDisposedException>();

            authenticationMethod.Dispose();
            deviceClient2.Dispose();
        }

        private async Task ReuseAuthenticationMethod_SingleDevice(IotHubClientTransportSettings transportSettings)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var authenticationMethod = new DeviceAuthenticationSasToken(testDevice.ConnectionString, disposeWithClient: false);

            var options = new IotHubClientOptions(transportSettings);

            // Create an instance of the device client, send a test message and then close and dispose it.
            var deviceClient = IotHubDeviceClient.Create(testDevice.IotHubHostName, authenticationMethod, options);
            using var message1 = new Client.Message();
            await deviceClient.SendEventAsync(message1).ConfigureAwait(false);
            await deviceClient.CloseAsync();
            deviceClient.Dispose();
            Logger.Trace("Test with instance 1 completed");

            // Perform the same steps again, reusing the previously created authentication method instance to ensure
            // that the SDK did not dispose the user supplied authentication method instance.
            var deviceClient2 = IotHubDeviceClient.Create(testDevice.IotHubHostName, authenticationMethod, options);
            using var message2 = new Client.Message();
            await deviceClient2.SendEventAsync(message2).ConfigureAwait(false);
            await deviceClient2.CloseAsync();
            deviceClient2.Dispose();
            Logger.Trace("Test with instance 2 completed, reused the previously created authentication method instance for the device client.");

            authenticationMethod.Dispose();
        }

        private async Task ReuseAuthenticationMethod_MuxedDevices(IotHubClientTransportSettings transportSettings, int devicesCount)
        {
            IList<TestDevice> testDevices = new List<TestDevice>();
            IList<IotHubDeviceClient> deviceClients = new List<IotHubDeviceClient>();
            IList<AuthenticationWithTokenRefresh> authenticationMethods = new List<AuthenticationWithTokenRefresh>();
            IList<AmqpConnectionStateChange> amqpConnectionStates = new List<AmqpConnectionStateChange>();

            // Set up amqp transport settings to multiplex all device sessions over the same amqp connection.
            var amqpTransportSettings = new IotHubClientAmqpSettings()
            {
                ConnectionPoolSettings = new AmqpConnectionPoolSettings
                {
                    Pooling = true,
                    MaxPoolSize = 1,
                },
            };

            for (int i = 0; i < devicesCount; i++)
            {
                TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
#pragma warning disable CA2000 // Dispose objects before losing scope - the authentication method is disposed at the end of the test.
                var authenticationMethod = new DeviceAuthenticationSasToken(testDevice.ConnectionString, disposeWithClient: false);
#pragma warning restore CA2000 // Dispose objects before losing scope

                testDevices.Add(testDevice);
                authenticationMethods.Add(authenticationMethod);
            }

            var options = new IotHubClientOptions(amqpTransportSettings);

            // Initialize the client instances, set the connection state change handler and open the connection.
            for (int i = 0; i < devicesCount; i++)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope - the client instance is disposed during the course of the test.
                var deviceClient = IotHubDeviceClient.Create(testDevices[i].IotHubHostName, authenticationMethods[i], options);
#pragma warning restore CA2000 // Dispose objects before losing scope

                var amqpConnectionStateChange = new AmqpConnectionStateChange(testDevices[i].Id, Logger);
                deviceClient.SetConnectionStateChangeHandler(amqpConnectionStateChange.ConnectionStateChangeHandler);
                amqpConnectionStates.Add(amqpConnectionStateChange);

                await deviceClient.OpenAsync().ConfigureAwait(false);
                deviceClients.Add(deviceClient);
            }

            // Close and dispose client instance 1.
            // The closed client should report a state of "disabled" while the rest of them should be connected.
            // This is to ensure that disposal on one multiplexed device doesn't cause cascading failures
            // in the rest of the devices on the same tcp connection.

            await deviceClients[0].CloseAsync().ConfigureAwait(false);
            deviceClients[0].Dispose();

            amqpConnectionStates[0].LastConnectionState.Should().Be(ConnectionState.Disabled);

            Logger.Trace($"{nameof(ReuseAuthenticationMethod_MuxedDevices)}: Confirming the rest of the multiplexed devices are online and operational.");

            bool notRecovered = true;
            var sw = Stopwatch.StartNew();
            while (notRecovered && sw.Elapsed < MaxWaitTime)
            {
                notRecovered = false;
                for (int i = 1; i < devicesCount; i++)
                {
                    if (amqpConnectionStates[i].LastConnectionState != ConnectionState.Connected)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        notRecovered = true;
                        break;
                    }
                }
            }

            notRecovered.Should().BeFalse();

            // Send a message through the rest of the multiplexed client instances.
            var message = new Client.Message();
            for (int i = 1; i < devicesCount; i++)
            {
                await deviceClients[i].SendEventAsync(message).ConfigureAwait(false);
                Logger.Trace($"Test with client {i} completed.");
            }
            message.Dispose();

            // Close and dispose all of the client instances.
            for (int i = 1; i < devicesCount; i++)
            {
                await deviceClients[i].CloseAsync().ConfigureAwait(false);
                deviceClients[i].Dispose();
            }

            deviceClients.Clear();
            amqpConnectionStates.Clear();

            // Initialize the client instances by reusing the created authentication methods and open the connection.
            for (int i = 0; i < devicesCount; i++)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope - the client instance is disposed at the end of the test.
                var deviceClient = IotHubDeviceClient.Create(
                    testDevices[i].IotHubHostName,
                    authenticationMethods[i],
                    options);
#pragma warning restore CA2000 // Dispose objects before losing scope

                var amqpConnectionStatusChange = new AmqpConnectionStateChange(testDevices[i].Id, Logger);
                deviceClient.SetConnectionStateChangeHandler(amqpConnectionStatusChange.ConnectionStateChangeHandler);
                amqpConnectionStates.Add(amqpConnectionStatusChange);

                await deviceClient.OpenAsync().ConfigureAwait(false);
                deviceClients.Add(deviceClient);
            }

            // Ensure that all clients are connected successfully, and the close and dispose the instances.
            // Also dispose the authentication methods created.
            for (int i = 0; i < devicesCount; i++)
            {
                amqpConnectionStates[i].LastConnectionState.Should().Be(ConnectionState.Connected);

                await deviceClients[i].CloseAsync();
                deviceClients[i].Dispose();
                authenticationMethods[i].Dispose();

                amqpConnectionStates[i].LastConnectionState.Should().Be(ConnectionState.Disabled);
            }
        }

        private class DeviceAuthenticationSasToken : DeviceAuthenticationWithTokenRefresh
        {
            private const string SasTokenTargetFormat = "{0}/devices/{1}";
            private readonly IotHubConnectionStringBuilder _connectionStringBuilder;

            private static readonly int s_suggestedSasTimeToLiveInSeconds = (int)TimeSpan.FromMinutes(30).TotalSeconds;
            private static readonly int s_sasRenewalBufferPercentage = 50;

            public DeviceAuthenticationSasToken(
                string connectionString,
                bool disposeWithClient)
                : base(GetDeviceIdFromConnectionString(connectionString), s_suggestedSasTimeToLiveInSeconds, s_sasRenewalBufferPercentage, disposeWithClient)
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
