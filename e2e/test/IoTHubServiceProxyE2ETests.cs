// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("Proxy")]
    public class IoTHubServiceProxyE2ETests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(IoTHubServiceProxyE2ETests)}_";
        private const string JobDeviceId = "JobsSample_Device";
        private const string JobTestTagName = "JobsSample_Tag";
        private const int JobTestTagValue = 100;
        private static TestLogging _log = TestLogging.GetInstance();
        private static string ConnectionString = Configuration.IoTHub.ConnectionString;
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private readonly ConsoleEventListener _listener;

        public IoTHubServiceProxyE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

#if !NETCOREAPP1_1

        [TestMethod]
        public async Task ServiceClient_Message_SendSingleMessage_WithProxy()
        {
            var transportSettings = new ServiceClientTransportSettings();
            transportSettings.AmqpProxy = new WebProxy(ProxyServerAddress);
            transportSettings.HttpProxy = new WebProxy(ProxyServerAddress);

            await SendSingleMessageService(transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RegistryManager_AddAndRemoveDevice_WithProxy()
        {
            var httpTransportSettings = new HttpTransportSettings();
            httpTransportSettings.Proxy = new WebProxy(ProxyServerAddress);

            await RegistryManager_AddDevice(httpTransportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task JobClient_ScheduleAndRunTwinJob_WithProxy()
        {
            var httpTransportSettings = new HttpTransportSettings();
            httpTransportSettings.Proxy = new WebProxy(ProxyServerAddress);

            await JobClient_ScheduleAndRunTwinJob(httpTransportSettings).ConfigureAwait(false);
        }

#endif

        private async Task SendSingleMessageService(ServiceClientTransportSettings transportSettings)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using (var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString))
            using (var serviceClient = ServiceClient.CreateFromConnectionString(ConnectionString, TransportType.Amqp, transportSettings))
            {
                TestMessage d2cMessage = ComposeC2DTestMessage();
                await serviceClient.SendAsync(testDevice.Id, d2cMessage.CloudMessage).ConfigureAwait(false);

                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task RegistryManager_AddDevice(HttpTransportSettings httpTransportSettings)
        {
            string deviceName = DevicePrefix + Guid.NewGuid();

            using (var registryManager = RegistryManager.CreateFromConnectionString(ConnectionString, httpTransportSettings))
            {
                await registryManager.AddDeviceAsync(new Device(deviceName)).ConfigureAwait(false);
                await registryManager.RemoveDeviceAsync(deviceName).ConfigureAwait(false);
            }
        }

        private async Task JobClient_ScheduleAndRunTwinJob(HttpTransportSettings httpTransportSettings)
        {
            string jobId = "JOBSAMPLE" + Guid.NewGuid().ToString();
            string query = $"DeviceId IN ['{JobDeviceId}']";

            var twin = new Twin(JobDeviceId)
            {
                Tags = new TwinCollection()
            };
            twin.Tags[JobTestTagName] = JobDeviceId;

            using (var jobClient = JobClient.CreateFromConnectionString(ConnectionString, httpTransportSettings))
            {
                JobResponse createJobResponse = await jobClient.ScheduleTwinUpdateAsync(jobId, query, twin, DateTime.UtcNow, (long)TimeSpan.FromMinutes(2).TotalSeconds).ConfigureAwait(false);
                JobResponse jobResponse = await jobClient.GetJobAsync(jobId).ConfigureAwait(false);
            }
        }

        private TestMessage ComposeC2DTestMessage()
        {
            var messageId = Guid.NewGuid().ToString();
            var payload = Guid.NewGuid().ToString();
            var p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeC2DTestMessage)}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                Properties = { ["property1"] = p1Value },
            };

            return new TestMessage
            {
                CloudMessage = message,
                Payload = payload,
                P1Value = p1Value,
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
