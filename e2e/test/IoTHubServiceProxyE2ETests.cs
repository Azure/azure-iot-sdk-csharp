// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    using TransportType = Microsoft.Azure.Devices.TransportType;

    [TestClass]
    [TestCategory("IoTHub-E2E")]
    [TestCategory("ProxyE2ETests")]
    public class IoTHubServiceProxyE2ETests
    {
        private const string DevicePrefix = "E2E_IoTHubServiceProxy_";
        private const string JobDeviceId = "JobsSample_Device";
        private const string JobTestTagName = "JobsSample_Tag";
        private const int JobTestTagValue = 100;
        private static TestLogging _log = TestLogging.GetInstance();
        private static string ConnectionString = Configuration.IoTHub.ConnectionString;
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;

        [TestMethod]
        public async Task ServiceClient_Message_SendSingleMessage_WithProxy()
        {
            ServiceClientTransportSettings transportSettings = new ServiceClientTransportSettings();
            transportSettings.AmqpProxy = new WebProxy(ProxyServerAddress);
            transportSettings.HttpProxy = new WebProxy(ProxyServerAddress);

            await SendSingleMessageService(transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RegistryManager_AddAndRemoveDevice_WithProxy()
        {
            HttpTransportSettings httpTransportSettings = new HttpTransportSettings();
            httpTransportSettings.Proxy = new WebProxy(ProxyServerAddress);

            await RegistryManager_AddDevice(httpTransportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task JobClient_ScheduleAndRunTwinJob_WithProxy()
        {
            HttpTransportSettings httpTransportSettings = new HttpTransportSettings();
            httpTransportSettings.Proxy = new WebProxy(ProxyServerAddress);

            await JobClient_ScheduleAndRunTwinJob(httpTransportSettings).ConfigureAwait(false);
        }

        private async Task SendSingleMessageService(ServiceClientTransportSettings transportSettings)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString);

            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(ConnectionString, TransportType.Amqp, transportSettings);

            string payload;
            string p1Value;
            Message testMessage = ComposeD2CTestMessage(out payload, out p1Value);
            await serviceClient.SendAsync(testDevice.Id, testMessage).ConfigureAwait(false);
        }

        private async Task RegistryManager_AddDevice(HttpTransportSettings httpTransportSettings)
        {
            string deviceName = DevicePrefix + Guid.NewGuid();

            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(ConnectionString, httpTransportSettings);
            await registryManager.AddDeviceAsync(new Device(deviceName)).ConfigureAwait(false);
            await registryManager.RemoveDeviceAsync(deviceName).ConfigureAwait(false);
        }

        private async Task JobClient_ScheduleAndRunTwinJob(HttpTransportSettings httpTransportSettings)
        {
            string jobId = "JOBSAMPLE" + Guid.NewGuid().ToString();
            string query = $"DeviceId IN ['{JobDeviceId}']";

            Twin twin = new Twin(JobDeviceId);
            twin.Tags = new TwinCollection();
            twin.Tags[JobTestTagName] = JobDeviceId;

            JobClient jobClient = JobClient.CreateFromConnectionString(ConnectionString, httpTransportSettings);
            JobResponse createJobResponse = await jobClient.ScheduleTwinUpdateAsync(jobId, query, twin, DateTime.UtcNow, (long)TimeSpan.FromMinutes(2).TotalSeconds).ConfigureAwait(false);
            JobResponse jobResponse = await jobClient.GetJobAsync(jobId).ConfigureAwait(false);
        }

       private Message ComposeD2CTestMessage(out string payload, out string p1Value)
        {
            payload = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeD2CTestMessage)}: payload='{payload}' p1Value='{p1Value}'");

            return new Message(Encoding.UTF8.GetBytes(payload))
            {
                Properties = { ["property1"] = p1Value }
            };
        }
    }
}
