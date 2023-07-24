using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Threading.Tasks;
using System;

namespace Microsoft.Azure.Devices.Client.Tests.OnBehalfOf
{
    public class EdgeDeviceOnBehalfOfTests
    {
        private static string _testKey => Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.Empty.ToString("N")));

        [TestMethod]
        public async Task ConnectDeviceOnBehalfOf()
        {
            string leafDeviceId = "test-leaf-device";

            DeviceConnectionSettings edgeDevice = new DeviceConnectionSettings("e4k-hub.azure-devices.net", "test-edge-device")
            {
                SharedAccessKey = _testKey
            };

            var edgeHubCs = await EdgeHubConnectionSettingsBuilder.Build(edgeDevice);

            IAuthenticationMethod leafAuth = new ClientAuthenticationWithSharedAccessKeyRefreshBehalfOf(
                edgeHubCs.SharedAccessKey!,
                edgeHubCs.DeviceId!,
                leafDeviceId,
                null,
                TimeSpan.FromMinutes(10),
                5);

            IotHubDeviceClient leafClient = new(edgeHubCs.HostName, leafAuth,
                new IotHubClientOptions(
                    new IotHubClientAmqpSettings
                    {
                        ConnectionPoolSettings = new AmqpConnectionPoolSettings()
                        {
                            UsePooling = true,
                            MaxPoolSize = 10
                        }
                    }));

            await leafClient.OpenAsync();
            long tick = Environment.TickCount;
            await leafClient.UpdateReportedPropertiesAsync(new ReportedProperties { ["tick"] = tick });
            var twin = await leafClient.GetTwinPropertiesAsync();
            Assert.NotNull(twin);
            Assert.NotNull(twin.Reported);
            Assert.NotNull(twin.Reported["tick"]);
            Assert.Equal(tick, twin.Reported["tick"]);
            await leafClient.CloseAsync();
        }

        [TestMethod]
        public async Task ConnectLeafModuleOnBehalfOf()
        {
            string leafDeviceId = "test-leaf-device";
            string edgeModuleId = "test-leaf-device-module";

            DeviceConnectionSettings edgeDevice = new DeviceConnectionSettings("e4k-hub.azure-devices.net", "test-edge-device")
            {
                SharedAccessKey = _testKey
            };

            var edgeHubCs = await EdgeHubConnectionSettingsBuilder.Build(edgeDevice);

            IAuthenticationMethod leafAuth = new ClientAuthenticationWithSharedAccessKeyRefreshBehalfOf(
                edgeHubCs.SharedAccessKey!,
                edgeHubCs.DeviceId!,
                leafDeviceId,
                edgeModuleId,
                TimeSpan.FromMinutes(10),
                5);

            IotHubModuleClient leafClient = new(edgeHubCs.HostName, leafAuth,
                new IotHubClientOptions(
                    new IotHubClientAmqpSettings
                    {
                        ConnectionPoolSettings = new AmqpConnectionPoolSettings()
                        {
                            UsePooling = true,
                            MaxPoolSize = 10
                        }
                    }));

            await leafClient.OpenAsync();
            long tick = Environment.TickCount;
            await leafClient.UpdateReportedPropertiesAsync(new ReportedProperties { ["tick"] = tick });
            var twin = await leafClient.GetTwinPropertiesAsync();
            Assert.NotNull(twin);
            Assert.NotNull(twin.Reported);
            Assert.NotNull(twin.Reported["tick"]);
            Assert.Equal(tick, twin.Reported["tick"]);
            await leafClient.CloseAsync();
        }

        [TestMethod]
        public async Task ConnectEdgeModuleOnBehalfOf()
        {
            string edgeDeviceId = "test-edge-device";
            string edgeModuleId = "test-edge-module";

            DeviceConnectionSettings edgeDevice = new DeviceConnectionSettings("e4k-hub.azure-devices.net", edgeDeviceId)
            {
                SharedAccessKey = _testKey
            };

            var edgeHubCs = await EdgeHubConnectionSettingsBuilder.Build(edgeDevice);

            IAuthenticationMethod leafAuth = new ClientAuthenticationWithSharedAccessKeyRefreshBehalfOf(
                edgeHubCs.SharedAccessKey!,
                edgeHubCs.DeviceId!,
                edgeDeviceId,
                edgeModuleId,
                TimeSpan.FromMinutes(10),
                5);

            IotHubModuleClient leafClient = new(edgeHubCs.HostName, leafAuth,
                new IotHubClientOptions(
                    new IotHubClientAmqpSettings
                    {
                        ConnectionPoolSettings = new AmqpConnectionPoolSettings()
                        {
                            UsePooling = true,
                            MaxPoolSize = 10
                        }
                    }));

            await leafClient.OpenAsync();
            long tick = Environment.TickCount;
            await leafClient.UpdateReportedPropertiesAsync(new ReportedProperties { ["tick"] = tick });
            var twin = await leafClient.GetTwinPropertiesAsync();
            Assert.NotNull(twin);
            Assert.NotNull(twin.Reported);
            Assert.NotNull(twin.Reported["tick"]);
            Assert.Equal(tick, twin.Reported["tick"]);
            await leafClient.CloseAsync();
        }
    }
}
