// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    using TransportType = Microsoft.Azure.Devices.TransportType;

    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("Proxy")]
    public class IoTHubServiceProxyE2ETests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(IoTHubServiceProxyE2ETests)}_";
        private const string JobDeviceId = "JobsSample_Device";
        private const string JobTestTagName = "JobsSample_Tag";
        private static string s_connectionString = Configuration.IoTHub.ConnectionString;
        private static string s_proxyServerAddress = Configuration.IoTHub.ProxyServerAddress;

#pragma warning disable CA1823
        private static TestLogging _log = TestLogging.GetInstance();
        private readonly ConsoleEventListener _listener;
#pragma warning restore CA1823

        public IoTHubServiceProxyE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task ServiceClient_Message_SendSingleMessage_WithProxy()
        {
            ServiceClientTransportSettings transportSettings = new ServiceClientTransportSettings();
            transportSettings.AmqpProxy = new WebProxy(s_proxyServerAddress);
            transportSettings.HttpProxy = new WebProxy(s_proxyServerAddress);

            await SendSingleMessageService(transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RegistryManager_AddAndRemoveDevice_WithProxy()
        {
            HttpTransportSettings httpTransportSettings = new HttpTransportSettings();
            httpTransportSettings.Proxy = new WebProxy(s_proxyServerAddress);

            await RegistryManager_AddDevice(httpTransportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task JobClient_ScheduleAndRunTwinJob_WithProxy()
        {
            HttpTransportSettings httpTransportSettings = new HttpTransportSettings();
            httpTransportSettings.Proxy = new WebProxy(s_proxyServerAddress);

            await JobClient_ScheduleAndRunTwinJob(httpTransportSettings).ConfigureAwait(false);
        }

        private async Task SendSingleMessageService(ServiceClientTransportSettings transportSettings)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString))
            using (ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(s_connectionString, TransportType.Amqp, transportSettings))
            {
                (Message testMessage, string messageId, string payload, string p1Value) = ComposeD2CTestMessage();
                await serviceClient.SendAsync(testDevice.Id, testMessage).ConfigureAwait(false);

                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task RegistryManager_AddDevice(HttpTransportSettings httpTransportSettings)
        {
            string deviceName = DevicePrefix + Guid.NewGuid();

            using (RegistryManager registryManager = RegistryManager.CreateFromConnectionString(s_connectionString, httpTransportSettings))
            {
                await registryManager.AddDeviceAsync(new Device(deviceName)).ConfigureAwait(false);
                await registryManager.RemoveDeviceAsync(deviceName).ConfigureAwait(false);
            }
        }

        private async Task JobClient_ScheduleAndRunTwinJob(HttpTransportSettings httpTransportSettings)
        {
            string jobId = "JOBSAMPLE" + Guid.NewGuid().ToString();
            string query = $"DeviceId IN ['{JobDeviceId}']";

            Twin twin = new Twin(JobDeviceId);
            twin.Tags = new TwinCollection();
            twin.Tags[JobTestTagName] = JobDeviceId;

            using (JobClient jobClient = JobClient.CreateFromConnectionString(s_connectionString, httpTransportSettings))
            {
                JobResponse createJobResponse = await jobClient.ScheduleTwinUpdateAsync(jobId, query, twin, DateTime.UtcNow, (long)TimeSpan.FromMinutes(2).TotalSeconds).ConfigureAwait(false);
                JobResponse jobResponse = await jobClient.GetJobAsync(jobId).ConfigureAwait(false);
            }
        }

        private static (Message message, string messageId, string payload, string p1Value) ComposeD2CTestMessage()
        {
            var messageId = Guid.NewGuid().ToString();
            var payload = Guid.NewGuid().ToString();
            var p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeD2CTestMessage)}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                Properties = { ["property1"] = p1Value }
            };

            return (message, messageId, payload, p1Value);
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
