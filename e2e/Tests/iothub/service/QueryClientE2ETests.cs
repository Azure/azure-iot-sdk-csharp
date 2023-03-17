// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    [DoNotParallelize] // creating jobs limits to running one at a time anyway, so reduce throttling conflicts
    public class QueryClientE2ETests : E2EMsTestBase
    {
        private readonly string _idPrefix = $"{nameof(QueryClientE2ETests)}_";

        // There is some latency between when a twin/job is created and when it can be queried. This
        // timeout is for how long to wait for this latency before failing the test.
        private readonly TimeSpan _queryableDelayTimeout = TimeSpan.FromMinutes(1);

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task TwinQuery_Works()
        {
            // arrange

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            await using TestDevice testDevice1 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);
            await using TestDevice testDevice2 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);

            string queryText = $"select * from devices where deviceId = '{testDevice1.Id}' OR deviceId = '{testDevice2.Id}'";

            // act

            await WaitForDevicesToBeQueryableAsync(serviceClient.Query, queryText, 2).ConfigureAwait(false);

            QueryResponse<ClientTwin> queryResponse = await serviceClient.Query.CreateAsync<ClientTwin>(queryText).ConfigureAwait(false);

            // assert

            (await queryResponse.MoveNextAsync().ConfigureAwait(false)).Should().BeTrue("Should have at least one page of jobs.");
            ClientTwin firstQueriedTwin = queryResponse.Current;

            firstQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id);
            (await queryResponse.MoveNextAsync().ConfigureAwait(false)).Should().BeTrue();
            ClientTwin secondQueriedTwin = queryResponse.Current;
            secondQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id);
            secondQueriedTwin.DeviceId.Should().NotBe(firstQueriedTwin.DeviceId);
            (await queryResponse.MoveNextAsync().ConfigureAwait(false)).Should().BeFalse();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task TwinQuery_CustomPaginationWorks()
        {
            // arrange

            var serviceClient = TestDevice.ServiceClient;
            await using TestDevice testDevice1 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);
            await using TestDevice testDevice2 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);
            await using TestDevice testDevice3 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);

            string queryText = $"select * from devices where deviceId = '{testDevice1.Id}' OR deviceId = '{testDevice2.Id}' OR deviceId = '{testDevice3.Id}'";
            var firstPageOptions = new QueryOptions
            {
                PageSize = 1
            };

            // act

            await WaitForDevicesToBeQueryableAsync(serviceClient.Query, queryText, 3).ConfigureAwait(false);

            QueryResponse<ClientTwin> queryResponse = await serviceClient.Query.CreateAsync<ClientTwin>(queryText, firstPageOptions).ConfigureAwait(false);

            // assert

            queryResponse.CurrentPage.Count().Should().Be(1);
            ClientTwin firstQueriedTwin = queryResponse.CurrentPage.First();
            firstQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id, testDevice3.Id);

            // consume the first page of results so the next MoveNextAsync gets a new page
            (await queryResponse.MoveNextAsync().ConfigureAwait(false)).Should().BeTrue("Should have at least one page of jobs.");

            var secondPageOptions = new QueryOptions
            {
                PageSize = 2
            };

            (await queryResponse.MoveNextAsync(secondPageOptions).ConfigureAwait(false)).Should().BeTrue();
            queryResponse.CurrentPage.Count().Should().Be(2);
            IEnumerator<ClientTwin> secondPageEnumerator = queryResponse.CurrentPage.GetEnumerator();
            secondPageEnumerator.MoveNext().Should().BeTrue();
            ClientTwin secondQueriedTwin = secondPageEnumerator.Current;
            secondQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id, testDevice3.Id);
            secondQueriedTwin.DeviceId.Should().NotBe(firstQueriedTwin.DeviceId);

            secondPageEnumerator.MoveNext().Should().BeTrue();
            ClientTwin thirdQueriedTwin = secondPageEnumerator.Current;
            thirdQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id, testDevice3.Id);
            thirdQueriedTwin.DeviceId.Should().NotBe(firstQueriedTwin.DeviceId);
            thirdQueriedTwin.DeviceId.Should().NotBe(secondQueriedTwin.DeviceId);

            secondPageEnumerator.MoveNext().Should().BeFalse();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task TwinQuery_IterateByItemAcrossPages()
        {
            // arrange

            var serviceClient = TestDevice.ServiceClient;
            await using TestDevice testDevice1 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);
            await using TestDevice testDevice2 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);
            await using TestDevice testDevice3 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);

            string queryText = $"select * from devices where deviceId = '{testDevice1.Id}' OR deviceId = '{testDevice2.Id}' OR deviceId = '{testDevice3.Id}'";

            // For this test, we want the query logic to have to fetch multiple pages of results. To force
            // that, set the page size to 1 when there are 3 total results to be queried.
            var queryOptions = new QueryOptions
            {
                PageSize = 1
            };

            // act

            await WaitForDevicesToBeQueryableAsync(serviceClient.Query, queryText, 3).ConfigureAwait(false);

            QueryResponse<ClientTwin> twinQuery = await serviceClient.Query
                .CreateAsync<ClientTwin>(queryText, queryOptions)
                .ConfigureAwait(false);

            // assert
            List<string> returnedTwinDeviceIds = new();
            while (await twinQuery.MoveNextAsync().ConfigureAwait(false))
            {
                ClientTwin queriedTwin = twinQuery.Current;
                returnedTwinDeviceIds.Add(queriedTwin.DeviceId);
            }

            var expectedDeviceIds = new List<string>() { testDevice1.Id, testDevice2.Id, testDevice3.Id };
            returnedTwinDeviceIds.Count.Should().Be(3);
            returnedTwinDeviceIds.Should().Contain(expectedDeviceIds);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task TwinQuery_IterateByItemWorksWithinPage()
        {
            // arrange

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            await using TestDevice testDevice1 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);
            await using TestDevice testDevice2 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);
            await using TestDevice testDevice3 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);

            string queryText = $"select * from devices where deviceId = '{testDevice1.Id}' OR deviceId = '{testDevice2.Id}' OR deviceId = '{testDevice3.Id}'";

            // For this test, we want the query logic to only fetch one page of results. To force
            // that, set the page size to 3 when there are 3 total results to be queried.
            var queryOptions = new QueryOptions
            {
                PageSize = 3
            };

            // act

            await WaitForDevicesToBeQueryableAsync(serviceClient.Query, queryText, 3).ConfigureAwait(false);

            QueryResponse<ClientTwin> twinQuery = await serviceClient.Query
                .CreateAsync<ClientTwin>(queryText, queryOptions)
                .ConfigureAwait(false);

            // assert
            List<string> returnedTwinDeviceIds = new();
            while (await twinQuery.MoveNextAsync().ConfigureAwait(false))
            {
                ClientTwin queriedTwin = twinQuery.Current;
                returnedTwinDeviceIds.Add(queriedTwin.DeviceId);
            }

            var expectedDeviceIds = new List<string>() { testDevice1.Id, testDevice2.Id, testDevice3.Id };
            returnedTwinDeviceIds.Count.Should().Be(3);
            returnedTwinDeviceIds.Should().Contain(expectedDeviceIds);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task JobQuery_QueryWorks()
        {
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);

            await ScheduleJobToBeQueriedAsync(serviceClient.ScheduledJobs, testDevice.Id).ConfigureAwait(false);

            string query = "SELECT * FROM devices.jobs";

            await WaitForJobToBeQueryableAsync(serviceClient.Query, query, 1).ConfigureAwait(false);

            QueryResponse<ScheduledJob> queryResponse = await serviceClient.Query.CreateAsync<ScheduledJob>(query).ConfigureAwait(false);
            (await queryResponse.MoveNextAsync().ConfigureAwait(false)).Should().BeTrue("Should have at least one page of jobs.");
            ScheduledJob queriedJob = queryResponse.Current;

            // Each IoT hub has a low limit for the number of parallel jobs allowed. Because of that,
            // tests in this suite are written to work even if the queried job isn't the one they created.
            // That's why these checks aren't more specific.
            queriedJob.JobId.Should().NotBeNull();
            queriedJob.JobType.Should().NotBe(JobType.Unknown);
            queriedJob.CreatedOnUtc.Should().NotBeNull();
            queriedJob.Status.Should().NotBe(JobStatus.Unknown);
            if (queriedJob.IsFinished)
            {
                queriedJob.StartedOnUtc.Should().NotBeNull();
                queriedJob.EndedOnUtc.Should().NotBeNull();
            }
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task JobQuery_QueryByTypeWorks()
        {
            var serviceClient = TestDevice.ServiceClient;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);

            await QueryClientE2ETests.ScheduleJobToBeQueriedAsync(serviceClient.ScheduledJobs, testDevice.Id).ConfigureAwait(false);
            await WaitForJobToBeQueryableAsync(serviceClient.Query, 1, null, null).ConfigureAwait(false);

            QueryResponse<ScheduledJob> queryResponse = await serviceClient.Query.CreateJobsQueryAsync().ConfigureAwait(false);
            (await queryResponse.MoveNextAsync().ConfigureAwait(false)).Should().BeTrue("Should have at least one page of jobs.");
            ScheduledJob queriedJob = queryResponse.Current;

            // Each IoT hub has a low limit for the number of parallel jobs allowed. Because of that,
            // tests in this suite are written to work even if the queried job isn't the one they created.
            // That's why these checks aren't more specific.
            queriedJob.JobId.Should().NotBeNull();
            queriedJob.JobType.Should().NotBe(JobType.Unknown);
            queriedJob.EndedOnUtc.Should().NotBeNull();
            queriedJob.CreatedOnUtc.Should().NotBeNull();
            queriedJob.Status.Should().NotBe(JobStatus.Unknown);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task RawQuery_QueryWorks()
        {
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);

            string query = "SELECT COUNT() as TotalNumberOfDevices FROM devices";

            QueryResponse<RawQuerySerializationClass> queryResponse = await serviceClient.Query.CreateAsync<RawQuerySerializationClass>(query).ConfigureAwait(false);
            (await queryResponse.MoveNextAsync().ConfigureAwait(false)).Should().BeTrue("Should have at least one page of jobs.");
            RawQuerySerializationClass queriedJob = queryResponse.Current;
            queriedJob.TotalNumberOfDevices.Should().BeGreaterOrEqualTo(0);
        }

        private async Task WaitForDevicesToBeQueryableAsync(QueryClient queryClient, string query, int expectedCount)
        {
            // There is some latency between the creation of the test devices and when they are queryable,
            // so keep executing the query until both devices are returned in the results or until a timeout.
            using var cancellationTokenSource = new CancellationTokenSource(_queryableDelayTimeout);
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            QueryResponse<ClientTwin> queryResponse = await queryClient.CreateAsync<ClientTwin>(query).ConfigureAwait(false);
            while (queryResponse.CurrentPage.Count() < expectedCount)
            {
                await Task.Delay(100).ConfigureAwait(false);
                queryResponse = await queryClient.CreateAsync<ClientTwin>(query).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested(); // timed out waiting for the devices to become queryable
            }
        }

        private async Task WaitForJobToBeQueryableAsync(QueryClient queryClient, string query, int expectedCount)
        {
            // There is some latency between the creation of the test devices and when they are queryable,
            // so keep executing the query until both devices are returned in the results or until a timeout.
            using var cancellationTokenSource = new CancellationTokenSource(_queryableDelayTimeout);
            QueryResponse<ScheduledJob> queryResponse = await queryClient.CreateAsync<ScheduledJob>(query).ConfigureAwait(false);
            while (queryResponse.CurrentPage.Count() < expectedCount)
            {
                cancellationTokenSource.Token.IsCancellationRequested.Should().BeFalse("timed out waiting for the devices to become queryable");
                await Task.Delay(100).ConfigureAwait(false);
                queryResponse = await queryClient.CreateAsync<ScheduledJob>(query).ConfigureAwait(false);
            }
        }

        private async Task WaitForJobToBeQueryableAsync(QueryClient queryClient, int expectedCount, JobType? jobType = null, JobStatus? status = null)
        {
            // There is some latency between the creation of the test devices and when they are queryable,
            // so keep executing the query until both devices are returned in the results or until a timeout.
            using var cancellationTokenSource = new CancellationTokenSource(_queryableDelayTimeout);
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            var options = new JobQueryOptions
            {
                JobType = jobType,
                JobStatus = status,
            };
            QueryResponse<ScheduledJob> queryResponse = await queryClient.CreateJobsQueryAsync(options).ConfigureAwait(false);
            while (queryResponse.CurrentPage.Count() < expectedCount)
            {
                cancellationTokenSource.Token.IsCancellationRequested.Should().BeFalse("timed out waiting for the devices to become queryable");
                await Task.Delay(100).ConfigureAwait(false);
                queryResponse = await queryClient.CreateJobsQueryAsync(options).ConfigureAwait(false);
            }
        }

        private static async Task ScheduleJobToBeQueriedAsync(ScheduledJobsClient jobsClient, string deviceId)
        {
            try
            {
                var twinUpdate = new ClientTwin();
                twinUpdate.Properties.Desired["key"] = "value";

                TwinScheduledJob scheduledJob = await jobsClient
                    .ScheduleTwinUpdateAsync("DeviceId IN ['" + deviceId + "']", twinUpdate, DateTimeOffset.UtcNow.AddMinutes(3))
                    .ConfigureAwait(false);
            }
            catch (IotHubServiceException ex) when (ex.StatusCode is (HttpStatusCode)429)
            {
                // Each IoT hub has a low limit for the number of parallel jobs allowed. Because of that,
                // tests in this suite are written to work even if the queried job isn't the one they created.
                VerboseTestLogger.WriteLine("Throttled when creating job. Will use existing job(s) to test query");
            }
        }
    }
}
