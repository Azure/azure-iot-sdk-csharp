// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Service")]
    [TestCategory("Proxy")]
    public class IotHubServiceProxyE2ETests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(IotHubServiceProxyE2ETests)}_";
        private const string JobDeviceId = "JobsSample_Device";
        private const string JobTestTagName = "JobsSample_Tag";
        private static readonly string s_connectionString = TestConfiguration.IotHub.ConnectionString;
        private static readonly string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;
        private const int MaxIterationWait = 30;
        private static readonly string s_devicePrefix = $"{nameof(IotHubServiceProxyE2ETests)}_";
        private static readonly TimeSpan s_waitDuration = TimeSpan.FromSeconds(5);

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ServiceClient_SendSingleMessage_WithProxy()
        {
            var options = new IotHubServiceClientOptions
            {
                Proxy = new WebProxy(TestConfiguration.IotHub.ProxyServerAddress),
            };

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix).ConfigureAwait(false);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString);
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);
            (OutgoingMessage testMessage, string messageId, string payload, string p1Value) = ComposeTelemetryMessage();
            await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
            await serviceClient.Messages.SendAsync(testDevice.Id, testMessage).ConfigureAwait(false);

            await serviceClient.Messages.CloseAsync().ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ServiceClientDevices_AddAndRemoveDevice_WithProxy()
        {
            var options = new IotHubServiceClientOptions
            {
                Proxy = new WebProxy(TestConfiguration.IotHub.ProxyServerAddress),
            };

            string deviceName = s_devicePrefix + Guid.NewGuid();

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);
            await serviceClient.Devices.CreateAsync(new Device(deviceName)).ConfigureAwait(false);
            await serviceClient.Devices.DeleteAsync(deviceName).ConfigureAwait(false);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("LongRunning")]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task JobClient_ScheduleAndRunTwinJob_WithProxy()
        {
            const string JobDeviceId = "JobsSample_Device";
            const string JobTestTagName = "JobsSample_Tag";

            var options = new IotHubServiceClientOptions
            {
                Proxy = new WebProxy(TestConfiguration.IotHub.ProxyServerAddress)
            };
            using var sc = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);

            var twin = new ClientTwin(JobDeviceId)
            {
                Tags = { { JobTestTagName, JobDeviceId } },
            };

            int tryCount = 0;

            while (true)
            {
                try
                {
                    TwinScheduledJob scheduledJob = await sc.ScheduledJobs
                        .ScheduleTwinUpdateAsync(
                            $"DeviceId IN ['{JobDeviceId}']",
                            twin,
                            DateTimeOffset.UtcNow,
                            new ScheduledJobsOptions
                            {
                                JobId = "JOBSAMPLE" + Guid.NewGuid().ToString(),
                                MaxExecutionTime = TimeSpan.FromMinutes(1),
                            })
                        .ConfigureAwait(false);
                    break;
                }
                // Concurrent jobs can be rejected, so implement a retry mechanism to handle conflicts with other tests
                catch (IotHubServiceException ex)
                    when (ex.StatusCode is (HttpStatusCode)429 && ++tryCount < MaxIterationWait)
                {
                    VerboseTestLogger.WriteLine($"ThrottlingException... waiting.");
                    await Task.Delay(s_waitDuration).ConfigureAwait(false);
                    continue;
                }
            }
        }

        private (OutgoingMessage message, string messageId, string payload, string p1Value) ComposeTelemetryMessage()
        {
            string messageId = Guid.NewGuid().ToString();
            string payload = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(ComposeTelemetryMessage)}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new OutgoingMessage(payload)
            {
                MessageId = messageId,
                Properties = { ["property1"] = p1Value }
            };

            return (message, messageId, payload, p1Value);
        }
    }
}
