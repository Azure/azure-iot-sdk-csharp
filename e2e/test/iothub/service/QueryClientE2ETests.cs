// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    /// <summary>
    /// E2E test class for all Query client operations.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class QueryClientE2eTests : E2EMsTestBase
    {
        private readonly string _idPrefix = $"{nameof(QueryClientE2eTests)}_";

        // There is some latency between when a twin/job is created and when it can be queried. This
        // timeout is for how long to wait for this latency before failing the test.
        private readonly TimeSpan _queryableDelayTimeout = TimeSpan.FromMinutes(1);

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task TwinQuery_Works()
        {
            // arrange

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            using TestDevice testDevice1 = await TestDevice.GetTestDeviceAsync(Logger, _idPrefix);
            using TestDevice testDevice2 = await TestDevice.GetTestDeviceAsync(Logger, _idPrefix);

            string queryText = $"select * from devices where deviceId = '{testDevice1.Id}' OR deviceId = '{testDevice2.Id}'";

            // act

            await WaitForDevicesToBeQueryable(serviceClient.Query, queryText, 2);

            QueryResponse<Twin> queryResponse = await serviceClient.Query.CreateAsync<Twin>(queryText);

            (await queryResponse.MoveNextAsync()).Should().BeTrue();
            Twin firstQueriedTwin = queryResponse.Current;

            // assert

            firstQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id);
            (await queryResponse.MoveNextAsync()).Should().BeTrue();
            Twin secondQueriedTwin = queryResponse.Current;
            secondQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id);
            secondQueriedTwin.DeviceId.Should().NotBe(firstQueriedTwin.DeviceId);
            (await queryResponse.MoveNextAsync()).Should().BeFalse();
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task TwinQuery_CustomPaginationWorks()
        {
            // arrange

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            using TestDevice testDevice1 = await TestDevice.GetTestDeviceAsync(Logger, _idPrefix);
            using TestDevice testDevice2 = await TestDevice.GetTestDeviceAsync(Logger, _idPrefix);
            using TestDevice testDevice3 = await TestDevice.GetTestDeviceAsync(Logger, _idPrefix);

            string queryText = $"select * from devices where deviceId = '{testDevice1.Id}' OR deviceId = '{testDevice2.Id}' OR deviceId = '{testDevice3.Id}'";
            QueryOptions firstPageOptions = new QueryOptions
            {
                PageSize = 1
            };

            // act

            await WaitForDevicesToBeQueryable(serviceClient.Query, queryText, 3);

            QueryResponse<Twin> queryResponse = await serviceClient.Query.CreateAsync<Twin>(queryText, firstPageOptions);

            // assert

            queryResponse.CurrentPage.Count().Should().Be(1);
            Twin firstQueriedTwin = queryResponse.CurrentPage.First();
            firstQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id, testDevice3.Id);

            // consume the first page of results so the next MoveNextAsync gets a new page
            (await queryResponse.MoveNextAsync()).Should().BeTrue();

            QueryOptions secondPageOptions = new QueryOptions
            {
                PageSize = 2
            };

            (await queryResponse.MoveNextAsync(secondPageOptions)).Should().BeTrue();
            queryResponse.CurrentPage.Count().Should().Be(2);
            IEnumerator<Twin> secondPageEnumerator = queryResponse.CurrentPage.GetEnumerator();
            secondPageEnumerator.MoveNext().Should().BeTrue();
            Twin secondQueriedTwin = secondPageEnumerator.Current;
            secondQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id, testDevice3.Id);
            secondQueriedTwin.DeviceId.Should().NotBe(firstQueriedTwin.DeviceId);

            secondPageEnumerator.MoveNext().Should().BeTrue();
            Twin thirdQueriedTwin = secondPageEnumerator.Current;
            thirdQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id, testDevice3.Id);
            thirdQueriedTwin.DeviceId.Should().NotBe(firstQueriedTwin.DeviceId);
            thirdQueriedTwin.DeviceId.Should().NotBe(secondQueriedTwin.DeviceId);

            secondPageEnumerator.MoveNext().Should().BeFalse();
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task JobQuery_QueryWorks()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _idPrefix);

            await ScheduleJobToBeQueried(serviceClient.ScheduledJobs, testDevice.Id);

            string query = "SELECT * FROM devices.jobs";

            await WaitForJobToBeQueryable(serviceClient.Query, query, 1);

            QueryResponse<ScheduledJob> queryResponse = await serviceClient.Query.CreateAsync<ScheduledJob>(query);
            (await queryResponse.MoveNextAsync()).Should().BeTrue();
            ScheduledJob queriedJob = queryResponse.Current;

            // Each IoT hub has a low limit for the number of parallel jobs allowed. Because of that,
            // tests in this suite are written to work even if the queried job isn't the one they created.
            // That's why these checks aren't more specific.
            queriedJob.JobId.Should().NotBeNull();
            queriedJob.JobType.Should().NotBeNull();
            queriedJob.CreatedTimeUtc.Should().NotBeNull();
            queriedJob.Status.Should().NotBeNull();
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task JobQuery_QueryByTypeWorks()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _idPrefix);

            await ScheduleJobToBeQueried(serviceClient.ScheduledJobs, testDevice.Id);

            JobType? type = null;
            JobStatus? status = null;

            await WaitForJobToBeQueryable(serviceClient.Query, 1, type, status);

            QueryResponse<ScheduledJob> queryResponse = await serviceClient.Query.CreateAsync(type, status);
            (await queryResponse.MoveNextAsync()).Should().BeTrue();
            ScheduledJob queriedJob = queryResponse.Current;

            // Each IoT hub has a low limit for the number of parallel jobs allowed. Because of that,
            // tests in this suite are written to work even if the queried job isn't the one they created.
            // That's why these checks aren't more specific.
            queriedJob.JobId.Should().NotBeNull();
            queriedJob.JobType.Should().NotBeNull();
            queriedJob.EndTimeUtc.Should().NotBeNull();
            queriedJob.CreatedTimeUtc.Should().NotBeNull();
            queriedJob.Status.Should().NotBeNull();
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task RawQuery_QueryWorks()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _idPrefix);

            string query = "SELECT COUNT() as TotalNumberOfDevices FROM devices";

            QueryResponse<RawQuerySerializationClass> queryResponse = await serviceClient.Query.CreateAsync<RawQuerySerializationClass>(query);
            (await queryResponse.MoveNextAsync()).Should().BeTrue();
            RawQuerySerializationClass queriedJob = queryResponse.Current;
            queriedJob.TotalNumberOfDevices.Should().BeGreaterOrEqualTo(0);
        }

        private async Task WaitForDevicesToBeQueryable(QueryClient queryClient, string query, int expectedCount)
        {
            // There is some latency between the creation of the test devices and when they are query-able,
            // so keep executing the query until both devices are returned in the results or until a timeout.
            using var cancellationTokenSource = new CancellationTokenSource(_queryableDelayTimeout);
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            QueryResponse<Twin> queryResponse = await queryClient.CreateAsync<Twin>(query);
            while (queryResponse.CurrentPage.Count() < expectedCount)
            {
                Thread.Sleep(100);
                queryResponse = await queryClient.CreateAsync<Twin>(query);
                cancellationToken.ThrowIfCancellationRequested(); // timed out waiting for the devices to become query-able
            }
        }

        private async Task WaitForJobToBeQueryable(QueryClient queryClient, string query, int expectedCount)
        {
            // There is some latency between the creation of the test devices and when they are query-able,
            // so keep executing the query until both devices are returned in the results or until a timeout.
            using var cancellationTokenSource = new CancellationTokenSource(_queryableDelayTimeout);
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            QueryResponse<ScheduledJob> queryResponse = await queryClient.CreateAsync<ScheduledJob>(query);
            while (queryResponse.CurrentPage.Count() < expectedCount)
            {
                Thread.Sleep(100);
                queryResponse = await queryClient.CreateAsync<ScheduledJob>(query);
                cancellationToken.ThrowIfCancellationRequested(); // timed out waiting for the devices to become query-able
            }
        }

        private async Task WaitForJobToBeQueryable(QueryClient queryClient, int expectedCount, JobType? type = null, JobStatus? status = null)
        {
            // There is some latency between the creation of the test devices and when they are query-able,
            // so keep executing the query until both devices are returned in the results or until a timeout.
            using var cancellationTokenSource = new CancellationTokenSource(_queryableDelayTimeout);
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            QueryResponse<ScheduledJob> queryResponse = await queryClient.CreateAsync(type, status);
            while (queryResponse.CurrentPage.Count() < expectedCount)
            {
                Thread.Sleep(100);
                queryResponse = await queryClient.CreateAsync(type, status);
                cancellationToken.ThrowIfCancellationRequested(); // timed out waiting for the devices to become query-able
            }
        }

        private async Task ScheduleJobToBeQueried(ScheduledJobsClient jobsClient, string deviceId)
        {
            Twin twinUpdate = new Twin();
            twinUpdate.Properties.Desired["key"] = "value";

            try
            {
                var scheduledTwinUpdate = new ScheduledTwinUpdate()
                {
                    Twin = twinUpdate,
                    QueryCondition = "DeviceId IN ['" + deviceId + "']",
                    StartTimeUtc = DateTime.UtcNow.AddMinutes(3),
                };

                await jobsClient.ScheduleTwinUpdateAsync(scheduledTwinUpdate);
            }
            catch (Common.Exceptions.ThrottlingException)
            {
                // Each IoT hub has a low limit for the number of parallel jobs allowed. Because of that,
                // tests in this suite are written to work even if the queried job isn't the one they created.
                Logger.Trace("Throttled when creating job. Will use existing job(s) to test query");
            }
        }
    }
}
