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

namespace Microsoft.Azure.Devices.E2ETests.Iothub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("Proxy")]
    public class IoTHubServiceProxyE2ETests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(IoTHubServiceProxyE2ETests)}_";
        private const string JobDeviceId = "JobsSample_Device";
        private const string JobTestTagName = "JobsSample_Tag";
        private static string s_connectionString = TestConfiguration.IoTHub.ConnectionString;
        private static string s_proxyServerAddress = TestConfiguration.IoTHub.ProxyServerAddress;
        private const int MaxIterationWait = 30;
        private static readonly TimeSpan _waitDuration = TimeSpan.FromSeconds(5);

        [LoggedTestMethod]
        public async Task ServiceClient_Message_SendSingleMessage_WithProxy()
        {
            ServiceClientTransportSettings transportSettings = new ServiceClientTransportSettings();
            transportSettings.AmqpProxy = new WebProxy(s_proxyServerAddress);
            transportSettings.HttpProxy = new WebProxy(s_proxyServerAddress);

            await SendSingleMessageService(transportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task RegistryManager_AddAndRemoveDevice_WithProxy()
        {
            HttpTransportSettings httpTransportSettings = new HttpTransportSettings();
            httpTransportSettings.Proxy = new WebProxy(s_proxyServerAddress);

            await RegistryManager_AddDevice(httpTransportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task JobClient_ScheduleAndRunTwinJob_WithProxy()
        {
            HttpTransportSettings httpTransportSettings = new HttpTransportSettings();
            httpTransportSettings.Proxy = new WebProxy(s_proxyServerAddress);

            await JobClient_ScheduleAndRunTwinJob(httpTransportSettings).ConfigureAwait(false);
        }

        private async Task SendSingleMessageService(ServiceClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
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
            Twin twin = new Twin(JobDeviceId);
            twin.Tags = new TwinCollection();
            twin.Tags[JobTestTagName] = JobDeviceId;

            using (JobClient jobClient = JobClient.CreateFromConnectionString(s_connectionString, httpTransportSettings))
            {
                int tryCount = 0;
                while (true)
                {
                    try
                    {
                        string jobId = "JOBSAMPLE" + Guid.NewGuid().ToString();
                        string query = $"DeviceId IN ['{JobDeviceId}']";
                        JobResponse createJobResponse = await jobClient.ScheduleTwinUpdateAsync(jobId, query, twin, DateTime.UtcNow, (long)TimeSpan.FromMinutes(2).TotalSeconds).ConfigureAwait(false);
                        break;
                    }
                    // Concurrent jobs can be rejected, so implement a retry mechanism to handle conflicts with other tests
                    catch (ThrottlingException) when (++tryCount < MaxIterationWait)
                    {
                        Logger.Trace($"ThrottlingException... waiting.");
                        await Task.Delay(_waitDuration).ConfigureAwait(false);
                        continue;
                    }
                }
            }
        }

        private (Message message, string messageId, string payload, string p1Value) ComposeD2CTestMessage()
        {
            var messageId = Guid.NewGuid().ToString();
            var payload = Guid.NewGuid().ToString();
            var p1Value = Guid.NewGuid().ToString();

            Logger.Trace($"{nameof(ComposeD2CTestMessage)}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                Properties = { ["property1"] = p1Value }
            };

            return (message, messageId, payload, p1Value);
        }
    }
}
