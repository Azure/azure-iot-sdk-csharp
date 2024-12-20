﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("Proxy")]
    [Ignore("Azure DevOps Windows test environment doesn't support proxies currently")]
    public class IoTHubServiceProxyE2ETests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(IoTHubServiceProxyE2ETests)}_";
        private const string JobDeviceId = "JobsSample_Device";
        private const string JobTestTagName = "JobsSample_Tag";
        private static string s_connectionString = TestConfiguration.IotHub.ConnectionString;
        private static string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;
        private const int MaxIterationWait = 180;
        private static readonly TimeSpan _waitDuration = TimeSpan.FromSeconds(2);

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ServiceClient_Message_SendSingleMessage_WithProxy()
        {
            var transportSettings = new ServiceClientTransportSettings();
            transportSettings.AmqpProxy = new WebProxy(s_proxyServerAddress);
            transportSettings.HttpProxy = new WebProxy(s_proxyServerAddress);

            await SendSingleMessageService(transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task RegistryManager_AddAndRemoveDevice_WithProxy()
        {
            var httpTransportSettings = new HttpTransportSettings();
            httpTransportSettings.Proxy = new WebProxy(s_proxyServerAddress);

            await RegistryManager_AddDevice(httpTransportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task JobClient_ScheduleAndRunTwinJob_WithProxy()
        {
            var httpTransportSettings = new HttpTransportSettings
            {
                Proxy = new WebProxy(s_proxyServerAddress)
            };

            await JobClient_ScheduleAndRunTwinJob(httpTransportSettings).ConfigureAwait(false);
        }

        private async Task SendSingleMessageService(ServiceClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString);
            using var serviceClient = ServiceClient.CreateFromConnectionString(s_connectionString, TransportType.Amqp, transportSettings);
            (Message testMessage, string messageId, string payload, string p1Value) = ComposeD2CTestMessage();

            using (testMessage)
            {
                await serviceClient.SendAsync(testDevice.Id, testMessage).ConfigureAwait(false);
            }

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task RegistryManager_AddDevice(HttpTransportSettings httpTransportSettings)
        {
            string deviceName = DevicePrefix + Guid.NewGuid();

            using var registryManager = RegistryManager.CreateFromConnectionString(s_connectionString, httpTransportSettings);
            await registryManager.AddDeviceAsync(new Device(deviceName)).ConfigureAwait(false);
            await registryManager.RemoveDeviceAsync(deviceName).ConfigureAwait(false);
        }

        private async Task JobClient_ScheduleAndRunTwinJob(HttpTransportSettings httpTransportSettings)
        {
            var twin = new Twin(JobDeviceId)
            {
                Tags = new TwinCollection()
            };
            twin.Tags[JobTestTagName] = JobDeviceId;

            using var jobClient = JobClient.CreateFromConnectionString(s_connectionString, httpTransportSettings);
            int tryCount = 0;
            while (true)
            {
                try
                {
                    string jobId = "JOBSAMPLE" + Guid.NewGuid().ToString();
                    string query = $"DeviceId IN ['{JobDeviceId}']";
                    JobResponse createJobResponse = await jobClient
                        .ScheduleTwinUpdateAsync(jobId, query, twin, DateTime.UtcNow, (long)TimeSpan.FromMinutes(2).TotalSeconds)
                        .ConfigureAwait(false);
                    break;
                }
                // Concurrent jobs can be rejected, so implement a retry mechanism to handle conflicts with other tests
                catch (ThrottlingException) when (++tryCount < MaxIterationWait)
                {
                    VerboseTestLogger.WriteLine($"ThrottlingException... waiting.");
                    await Task.Delay(_waitDuration).ConfigureAwait(false);
                    continue;
                }
            }
        }

        private (Message message, string messageId, string payload, string p1Value) ComposeD2CTestMessage()
        {
            string messageId = Guid.NewGuid().ToString();
            string payload = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(ComposeD2CTestMessage)}: messageId='{messageId}' payload='{payload.Substring(0, 32)}' p1Value='{p1Value}'");
            var message = new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                Properties = { ["property1"] = p1Value }
            };

            return (message, messageId, payload, p1Value);
        }
    }
}
