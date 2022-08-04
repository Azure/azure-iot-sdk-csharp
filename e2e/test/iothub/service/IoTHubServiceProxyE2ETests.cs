// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("Proxy")]
    public class IoTHubServiceProxyE2ETests : E2EMsTestBase
    {
        private const string JobDeviceId = "JobsSample_Device";
        private const string JobTestTagName = "JobsSample_Tag";
        private const int MaxIterationWait = 30;
        private static readonly string s_devicePrefix = $"{nameof(IoTHubServiceProxyE2ETests)}_";
        private static readonly string s_connectionString = TestConfiguration.IoTHub.ConnectionString;
        private static readonly string s_proxyServerAddress = TestConfiguration.IoTHub.ProxyServerAddress;
        private static readonly TimeSpan s_waitDuration = TimeSpan.FromSeconds(5);

        [LoggedTestMethod]
        public async Task ServiceClient_Message_SendSingleMessage_WithProxy()
        {
            var transportSettings = new ServiceClientTransportSettings
            {
                AmqpProxy = new WebProxy(s_proxyServerAddress),
                HttpProxy = new WebProxy(s_proxyServerAddress)
            };

            await SendSingleMessageService(transportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task RegistryManager_AddAndRemoveDevice_WithProxy()
        {
            var options = new IotHubServiceClientOptions
            {
                Proxy = new WebProxy(s_proxyServerAddress),
            };

            await IoTHubServiceProxyE2ETests.RegistryManager_AddDevice(options).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task JobClient_ScheduleAndRunTwinJob_WithProxy()
        {
            var httpTransportSettings = new HttpTransportSettings
            {
                Proxy = new WebProxy(s_proxyServerAddress),
            };

            await JobClient_ScheduleAndRunTwinJob(httpTransportSettings).ConfigureAwait(false);
        }

        private async Task SendSingleMessageService(ServiceClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix).ConfigureAwait(false);
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(testDevice.ConnectionString);
            using var serviceClient = ServiceClient.CreateFromConnectionString(s_connectionString, TransportType.Amqp, transportSettings);
            (Message testMessage, string messageId, string payload, string p1Value) = ComposeD2CTestMessage();
            await serviceClient.SendAsync(testDevice.Id, testMessage).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private static async Task RegistryManager_AddDevice(IotHubServiceClientOptions options)
        {
            string deviceName = s_devicePrefix + Guid.NewGuid();

            using var serviceClient = new IotHubServiceClient(s_connectionString, options);
            await serviceClient.Devices.CreateAsync(new Device(deviceName)).ConfigureAwait(false);
            await serviceClient.Devices.DeleteAsync(deviceName).ConfigureAwait(false);
        }

        private async Task JobClient_ScheduleAndRunTwinJob(HttpTransportSettings httpTransportSettings)
        {
            var twin = new Twin(JobDeviceId)
            {
                Tags = new TwinCollection(),
            };
            twin.Tags[JobTestTagName] = JobDeviceId;
            var options = new IotHubServiceClientOptions
            {
                Proxy = httpTransportSettings.Proxy
            };
            using var sc = new IotHubServiceClient(s_connectionString, options);
            int tryCount = 0;
            while (true)
            {
                try
                {
                    string jobId = "JOBSAMPLE" + Guid.NewGuid().ToString();
                    string query = $"DeviceId IN ['{JobDeviceId}']";
                    var twinUpdate = new ScheduledTwinUpdate
                    {
                        QueryCondition = query,
                        Twin = twin,
                        StartTimeUtc = DateTime.UtcNow
                    };
                    var ScheduledTwinUpdateOptions = new ScheduledJobsOptions
                    {
                        JobId = jobId,
                        MaxExecutionTime = TimeSpan.FromMinutes(2)
                    };
                    ScheduledJob scheduledJob = await sc.ScheduledJobs
                        .ScheduleTwinUpdateAsync(twinUpdate, ScheduledTwinUpdateOptions)
                        .ConfigureAwait(false);
                    break;
                }
                // Concurrent jobs can be rejected, so implement a retry mechanism to handle conflicts with other tests
                catch (ThrottlingException) when (++tryCount < MaxIterationWait)
                {
                    Logger.Trace($"ThrottlingException... waiting.");
                    await Task.Delay(s_waitDuration).ConfigureAwait(false);
                    continue;
                }
            }
        }

        private (Message message, string messageId, string payload, string p1Value) ComposeD2CTestMessage()
        {
            string messageId = Guid.NewGuid().ToString();
            string payload = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();

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
