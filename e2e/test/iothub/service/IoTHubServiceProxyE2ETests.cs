// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("Proxy")]
    public class IotHubServiceProxyE2ETests : E2EMsTestBase
    {
        private const int MaxIterationWait = 30;
        private static readonly string s_devicePrefix = $"{nameof(IotHubServiceProxyE2ETests)}_";
        private static readonly TimeSpan s_waitDuration = TimeSpan.FromSeconds(5);

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ServiceClient_SendSingleMessage_WithProxy()
        {
            var options = new IotHubServiceClientOptions
            {
                Proxy = new WebProxy(TestConfiguration.IoTHub.ProxyServerAddress),
            };

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix).ConfigureAwait(false);
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString);
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString, options);
            (Message testMessage, string messageId, string payload, string p1Value) = ComposeTelemetryMessage();
            await serviceClient.Messaging.OpenAsync().ConfigureAwait(false);
            await serviceClient.Messaging.SendAsync(testDevice.Id, testMessage).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.Messaging.CloseAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ServiceClientDevices_AddAndRemoveDevice_WithProxy()
        {
            var options = new IotHubServiceClientOptions
            {
                Proxy = new WebProxy(TestConfiguration.IoTHub.ProxyServerAddress),
            };

            string deviceName = s_devicePrefix + Guid.NewGuid();

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString, options);
            await serviceClient.Devices.CreateAsync(new Device(deviceName)).ConfigureAwait(false);
            await serviceClient.Devices.DeleteAsync(deviceName).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [Ignore]
        public async Task JobClient_ScheduleAndRunTwinJob_WithProxy()
        {
            const string JobDeviceId = "JobsSample_Device";
            const string JobTestTagName = "JobsSample_Tag";

            var options = new IotHubServiceClientOptions
            {
                Proxy = new WebProxy(TestConfiguration.IoTHub.ProxyServerAddress)
            };
            using var sc = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString, options);

            var twin = new Twin(JobDeviceId)
            {
                Tags = new TwinCollection(),
            };
            twin.Tags[JobTestTagName] = JobDeviceId;

            int tryCount = 0;

            while (true)
            {
                try
                {
                    ScheduledJob scheduledJob = await sc.ScheduledJobs
                        .ScheduleTwinUpdateAsync(
                            new ScheduledTwinUpdate
                            {
                                QueryCondition = $"DeviceId IN ['{JobDeviceId}']",
                                Twin = twin,
                                StartOn = DateTimeOffset.UtcNow,
                            },
                            new ScheduledJobsOptions
                            {
                                JobId = "JOBSAMPLE" + Guid.NewGuid().ToString(),
                                MaxExecutionTime = TimeSpan.FromMinutes(1),
                            })
                        .ConfigureAwait(false);
                    break;
                }
                // Concurrent jobs can be rejected, so implement a retry mechanism to handle conflicts with other tests
                catch (IotHubServiceException ex) when (ex.IotHubStatusCode is IotHubStatusCode.ThrottlingException && ++tryCount < MaxIterationWait)
                {
                    Logger.Trace($"ThrottlingException... waiting.");
                    await Task.Delay(s_waitDuration).ConfigureAwait(false);
                    continue;
                }
            }
        }

        private (Message message, string messageId, string payload, string p1Value) ComposeTelemetryMessage()
        {
            string messageId = Guid.NewGuid().ToString();
            string payload = Guid.NewGuid().ToString();
            string p1Value = Guid.NewGuid().ToString();

            Logger.Trace($"{nameof(ComposeTelemetryMessage)}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}'");
            var message = new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                Properties = { ["property1"] = p1Value }
            };

            return (message, messageId, payload, p1Value);
        }
    }
}
