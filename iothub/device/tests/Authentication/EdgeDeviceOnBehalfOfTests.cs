using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Threading.Tasks;
using System;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Authentication;

namespace Microsoft.Azure.Devices.Client.Tests.OnBehalfOf
{
    [TestClass]
    public class EdgeDeviceOnBehalfOfTests
    {
        private static string _testKey => Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.Empty.ToString("N")));

        [TestMethod]
        public async Task ConnectDeviceOnBehalfOf()
        {
            string leafDeviceId = "test-leaf-device";

            var edgeHubCs = new IotHubConnectionString("e4k-hub.azure-devices.net", null, "test-edge-device", "$edgeHub", null, _testKey, null);

            IAuthenticationMethod leafAuth = new ClientAuthenticationForEdgeHubOnBehalfOf(
                edgeHubCs.SharedAccessKey!,
                edgeHubCs.DeviceId!,
                leafDeviceId,
                null,
                TimeSpan.FromMinutes(10),
                5);

            IotHubDeviceClient leafClient = new(edgeHubCs.IotHubHostName, leafAuth,
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
            twin.Should().NotBeNull();
            twin.Reported.Should().NotBeNull();
            twin.Reported["tick"].Should().NotBeNull();
            tick.Should().Be((long)twin.Reported["tick"]);
            await leafClient.CloseAsync();
            await leafClient.DisposeAsync();
        }

        [TestMethod]
        public async Task ConnectLeafModuleOnBehalfOf()
        {
            string leafDeviceId = "test-leaf-device";
            string edgeModuleId = "test-leaf-device-module";

            var edgeHubCs = new IotHubConnectionString("e4k-hub.azure-devices.net", null, "test-edge-device", "$edgeHub", null, _testKey, null);

            IAuthenticationMethod leafAuth = new ClientAuthenticationForEdgeHubOnBehalfOf(
                edgeHubCs.SharedAccessKey!,
                edgeHubCs.DeviceId!,
                leafDeviceId,
                edgeModuleId,
                TimeSpan.FromMinutes(10),
                5);

            IotHubModuleClient leafClient = new(edgeHubCs.IotHubHostName, leafAuth,
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
            twin.Should().NotBeNull();
            twin.Reported.Should().NotBeNull();
            twin.Reported["tick"].Should().NotBeNull();
            tick.Should().Be((long)twin.Reported["tick"]);
            await leafClient.CloseAsync();
            await leafClient.DisposeAsync();
        }

        [TestMethod]
        public async Task ConnectEdgeModuleOnBehalfOf()
        {
            string edgeDeviceId = "test-edge-device";
            string edgeModuleId = "test-edge-module";

            var edgeHubCs = new IotHubConnectionString("e4k-hub.azure-devices.net", null, "test-edge-device", "$edgeHub", null, _testKey, null);

            IAuthenticationMethod leafAuth = new ClientAuthenticationForEdgeHubOnBehalfOf(
                edgeHubCs.SharedAccessKey!,
                edgeHubCs.DeviceId!,
                edgeDeviceId,
                edgeModuleId,
                TimeSpan.FromMinutes(10),
                5);

            IotHubModuleClient leafClient = new(edgeHubCs.IotHubHostName, leafAuth,
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
            twin.Should().NotBeNull();
            twin.Reported.Should().NotBeNull();
            twin.Reported["tick"].Should().NotBeNull();
            tick.Should().Be((long)twin.Reported["tick"]);
            await leafClient.CloseAsync();
            await leafClient.DisposeAsync();
        }
    }
}
