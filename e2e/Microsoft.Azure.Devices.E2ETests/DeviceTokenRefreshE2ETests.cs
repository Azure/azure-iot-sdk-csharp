// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    public class DeviceTokenRefreshE2ETests
    {
        private const string DevicePrefix = "E2E_Message_TokenRefresh_";

        [TestMethod]
        public async Task DeviceClient_TokenIsRefreshed_Ok_Http()
        {
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Http1);
        }
        
        [TestMethod]
        public async Task DeviceClient_TokenIsRefreshed_Ok_Amqp()
        {
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Amqp);
        }

        [TestMethod]
        public async Task DeviceClient_TokenIsRefreshed_Fails_Mqtt()
        {
            await DeviceClient_TokenIsRefreshed_Internal(Client.TransportType.Mqtt);
        }

        private async Task DeviceClient_TokenIsRefreshed_Internal(Client.TransportType transport)
        {
            var builder = IotHubConnectionStringBuilder.Create(Configuration.IoTHub.ConnectionString);

            RegistryManager rm = await TestUtil.GetRegistryManagerAsync(DevicePrefix);
            int ttl = 6;
            int buffer = 50;

            try
            {
                Device device = await CreateDeviceClientAsync(rm);

                var refresher = new TestTokenRefresher(
                    device.Id, 
                    device.Authentication.SymmetricKey.PrimaryKey, 
                    ttl, 
                    buffer);

                DeviceClient deviceClient = 
                    DeviceClient.Create(builder.HostName, refresher, transport);

                var message = new Client.Message(Encoding.UTF8.GetBytes("Hello"));

                // Create the first Token.
                Console.WriteLine($"[{DateTime.UtcNow}] OpenAsync");
                await deviceClient.OpenAsync();
                Console.WriteLine($"[{DateTime.UtcNow}] SendEventAsync (1)");
                await deviceClient.SendEventAsync(message);

                int countAfterOpenAndFirstSend = refresher.SafeCreateNewTokenCallCount;
                Assert.IsTrue(countAfterOpenAndFirstSend >= 1, $"[{DateTime.UtcNow}] Token should have been refreshed at least once.");

                Console.WriteLine($"[{DateTime.UtcNow}] Waiting {ttl} seconds.");

                // Wait for the Token to expire.
                await Task.Delay(ttl * 1000);

                Console.WriteLine($"[{DateTime.UtcNow}] SendEventAsync (2)");
                await deviceClient.SendEventAsync(message);

                // Ensure that the token was refreshed.
                if (transport == Client.TransportType.Mqtt)
                {
                    // This is not currently supported for MQTT unless the connection is dropped and re-established.
                    Assert.IsTrue(
                        refresher.SafeCreateNewTokenCallCount >= countAfterOpenAndFirstSend,
                        $"[{DateTime.UtcNow}] Token should have been refreshed after TTL expired.");
                }
                else
                {
                    Assert.IsTrue(
                        refresher.SafeCreateNewTokenCallCount >= countAfterOpenAndFirstSend + 1,
                        $"[{DateTime.UtcNow}] Token should have been refreshed after TTL expired.");
                }

                Console.WriteLine($"[{DateTime.UtcNow}] CloseAsync");
                await deviceClient.CloseAsync();
            }
            finally
            {
                await TestUtil.UnInitializeEnvironment(rm);
            }
        }

        private async Task<Device> CreateDeviceClientAsync(RegistryManager registryManager)
        {
            string deviceName = DevicePrefix + Guid.NewGuid();
            Console.WriteLine($"Creating device {deviceName}");
            Device ret = await registryManager.AddDeviceAsync(new Device(deviceName));

            Console.WriteLine("Pausing before using the device.");
            await Task.Delay(5000).ConfigureAwait(false);

            return ret;
        }

        private class TestTokenRefresher : DeviceAuthenticationWithTokenRefresh
        {
            private int _callCount = 0;
            private string _key;

            public int SafeCreateNewTokenCallCount
            {
                get
                {
                    return _callCount;
                }
            }

            public TestTokenRefresher(string deviceId, string key) : base(deviceId)
            {
                _key = key;
            }

            public TestTokenRefresher(
                string deviceId, 
                string key, 
                int suggestedTimeToLive, 
                int timeBufferPercentage) 
                : base(deviceId, suggestedTimeToLive, timeBufferPercentage)
            {
                _key = key;
            }

            protected override Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLive)
            {
                Console.WriteLine($"[{DateTime.UtcNow}] Refresher: Creating new token {_callCount}");

                var builder = new SharedAccessSignatureBuilder()
                {
                    Key = _key,
                    TimeToLive = TimeSpan.FromSeconds(suggestedTimeToLive),
                    Target = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}/devices/{1}",
                        iotHub,
                        WebUtility.UrlEncode(DeviceId)),
                };

                _callCount++;

                string token = builder.ToSignature();
                Console.WriteLine($"Token: {token}");
                return Task.FromResult(token);
            }
        }
    }
}
