// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
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

            AsyncPageable<ClientTwin> queryResponse = serviceClient.Query.Create<ClientTwin>(queryText);
            IAsyncEnumerator<ClientTwin> enumerator = queryResponse.GetAsyncEnumerator();

            // assert
            (await enumerator.MoveNextAsync().ConfigureAwait(false)).Should().BeTrue("Should have at least one page of jobs.");
            ClientTwin firstQueriedTwin = enumerator.Current;

            firstQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id);
            (await enumerator.MoveNextAsync().ConfigureAwait(false)).Should().BeTrue();
            ClientTwin secondQueriedTwin = enumerator.Current;
            secondQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id);
            secondQueriedTwin.DeviceId.Should().NotBe(firstQueriedTwin.DeviceId);
            (await enumerator.MoveNextAsync().ConfigureAwait(false)).Should().BeFalse();
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

            // act
            await WaitForDevicesToBeQueryableAsync(serviceClient.Query, queryText, 3).ConfigureAwait(false);

            AsyncPageable<ClientTwin> queryResponse = serviceClient.Query.
                Create<ClientTwin>(queryText);
            await using IAsyncEnumerator<Page<ClientTwin>> enumerator = queryResponse.AsPages(null, 1).GetAsyncEnumerator();
            (await enumerator.MoveNextAsync().ConfigureAwait(false)).Should().BeTrue("Should have at least one page of jobs.");

            // assert
            Page<ClientTwin> currentPage = enumerator.Current;
            currentPage.Values.Count.Should().Be(1);
            currentPage.Values[0].DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id, testDevice3.Id);


            // restart the query, but with a page size of 3 this time
            queryResponse = serviceClient.Query.
                Create<ClientTwin>(queryText);
            await using IAsyncEnumerator<Page<ClientTwin>> nextEnumerator = queryResponse.AsPages(null, 3).GetAsyncEnumerator();

            // consume the first page of results so the next MoveNextAsync gets a new page
            (await nextEnumerator.MoveNextAsync().ConfigureAwait(false)).Should().BeTrue("Should have at least one page of jobs.");

            currentPage = enumerator.Current;
            currentPage.Values.Count.Should().Be(3);
            IEnumerator<ClientTwin> pageContentsEnumerator = currentPage.Values.GetEnumerator();
            pageContentsEnumerator.MoveNext().Should().BeTrue();

            ClientTwin firstQueriedTwin = pageContentsEnumerator.Current;
            firstQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id, testDevice3.Id);
            pageContentsEnumerator.MoveNext().Should().BeTrue();

            ClientTwin secondQueriedTwin = pageContentsEnumerator.Current;
            secondQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id, testDevice3.Id);
            pageContentsEnumerator.MoveNext().Should().BeTrue();

            ClientTwin thirdQueriedTwin = pageContentsEnumerator.Current;
            thirdQueriedTwin.DeviceId.Should().BeOneOf(testDevice1.Id, testDevice2.Id, testDevice3.Id);

            (await nextEnumerator.MoveNextAsync().ConfigureAwait(false)).Should().BeFalse("After 3 query results in one page, there should not be a second page");
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

            // act

            await WaitForDevicesToBeQueryableAsync(serviceClient.Query, queryText, 3).ConfigureAwait(false);

            // For this test, we want the query logic to have to fetch multiple pages of results. To force
            // that, set the page size to 1 when there are 3 total results to be queried.
            IAsyncEnumerable<Page<ClientTwin>> twinPages = serviceClient.Query.
                Create<ClientTwin>(queryText)
                .AsPages(null, 1);

            // assert

            var returnedTwinDeviceIds = new List<string>();
            await foreach (Page<ClientTwin> queriedTwinPage in twinPages)
            {
                foreach (ClientTwin queriedTwin in queriedTwinPage.Values)
                { 
                    returnedTwinDeviceIds.Add(queriedTwin.DeviceId);
                }

                queriedTwinPage.GetRawResponse().Dispose();
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


            // act

            await WaitForDevicesToBeQueryableAsync(serviceClient.Query, queryText, 3).ConfigureAwait(false);

            AsyncPageable<ClientTwin> twinQuery = serviceClient.Query
                .Create<ClientTwin>(queryText);

            // assert

            // For this test, we want the query logic to only fetch one page of results. To force
            // that, set the page size to 3 when there are 3 total results to be queried.
            IAsyncEnumerable<Page<ClientTwin>> twinPages = twinQuery.AsPages(null, 3);
            var returnedTwinDeviceIds = new List<string>();
            await foreach (Page<ClientTwin> queriedTwinPage in twinPages)
            {
                foreach (ClientTwin queriedTwin in queriedTwinPage.Values)
                {
                    returnedTwinDeviceIds.Add(queriedTwin.DeviceId);
                }

                queriedTwinPage.GetRawResponse().Dispose();
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

            string jobId = await ScheduleJobToBeQueriedAsync(serviceClient.ScheduledJobs, testDevice.Id).ConfigureAwait(false);

            string query = "SELECT * FROM devices.jobs";
            await WaitForJobToBeQueryableAsync(serviceClient.Query, query, 1).ConfigureAwait(false);

            AsyncPageable<ScheduledJob> queryResponse = serviceClient.Query.Create<ScheduledJob>(query);
            IAsyncEnumerator<ScheduledJob> enumerator = queryResponse.GetAsyncEnumerator();
            (await enumerator.MoveNextAsync().ConfigureAwait(false)).Should().BeTrue("Should have at least one page of jobs.");
            ScheduledJob queriedJob = enumerator.Current;

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

            await ScheduleJobToBeQueriedAsync(serviceClient.ScheduledJobs, testDevice.Id).ConfigureAwait(false);
            await WaitForJobToBeQueryableAsync(serviceClient.Query, 1, null, null).ConfigureAwait(false);

            AsyncPageable<ScheduledJob> queryResponse = serviceClient.Query.CreateJobsQuery();
            IAsyncEnumerator<ScheduledJob> enumerator = queryResponse.GetAsyncEnumerator();
            (await enumerator.MoveNextAsync().ConfigureAwait(false)).Should().BeTrue("Should have at least one page of jobs.");
            ScheduledJob queriedJob = enumerator.Current;

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

            AsyncPageable<RawQuerySerializationClass> queryResponse = serviceClient.Query.Create<RawQuerySerializationClass>(query);
            await using IAsyncEnumerator<RawQuerySerializationClass> enumerator = queryResponse.GetAsyncEnumerator();
            (await enumerator.MoveNextAsync().ConfigureAwait(false)).Should().BeTrue("Should have at least one page of jobs.");
            RawQuerySerializationClass queriedJob = enumerator.Current;
            queriedJob.TotalNumberOfDevices.Should().BeGreaterOrEqualTo(0);
        }

        private async Task WaitForDevicesToBeQueryableAsync(QueryClient queryClient, string query, int expectedCount)
        {
            // There is some latency between the creation of the test devices and when they are queryable,
            // so keep executing the query until both devices are returned in the results or until a timeout.
            using var cancellationTokenSource = new CancellationTokenSource(_queryableDelayTimeout);
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            IAsyncEnumerator<Page<ClientTwin>> enumerator = queryClient.Create<ClientTwin>(query).AsPages().GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
            while (enumerator.Current.Values.Count < expectedCount)
            {
                await Task.Delay(100).ConfigureAwait(false);
                await enumerator.DisposeAsync().ConfigureAwait(false);
                enumerator = queryClient.Create<ClientTwin>(query).AsPages().GetAsyncEnumerator();
                await enumerator.MoveNextAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested(); // timed out waiting for the devices to become queryable
            }

            await enumerator.DisposeAsync().ConfigureAwait(false);
        }

        private async Task WaitForJobToBeQueryableAsync(QueryClient queryClient, string query, int expectedCount)
        {
            // There is some latency between the creation of the test devices and when they are queryable,
            // so keep executing the query until both devices are returned in the results or until a timeout.
            using var cancellationTokenSource = new CancellationTokenSource(_queryableDelayTimeout);
            IAsyncEnumerator<Page<ScheduledJob>> enumerator = queryClient.Create<ScheduledJob>(query).AsPages().GetAsyncEnumerator();
            await enumerator.MoveNextAsync().ConfigureAwait(false);
            while (enumerator.Current.Values.Count < expectedCount)
            {
                await Task.Delay(100).ConfigureAwait(false);
                cancellationTokenSource.Token.IsCancellationRequested.Should().BeFalse("timed out waiting for the devices to become queryable");
                await enumerator.DisposeAsync().ConfigureAwait(false); 
                enumerator = queryClient.Create<ScheduledJob>(query).AsPages().GetAsyncEnumerator();
                await enumerator.MoveNextAsync().ConfigureAwait(false);
            }

            await enumerator.DisposeAsync().ConfigureAwait(false);
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
            IAsyncEnumerator<Page<ScheduledJob>> enumerator = queryClient.CreateJobsQuery(options).AsPages().GetAsyncEnumerator();
            await enumerator.MoveNextAsync().ConfigureAwait(false);
            while (enumerator.Current.Values.Count < expectedCount)
            {
                cancellationTokenSource.Token.IsCancellationRequested.Should().BeFalse("timed out waiting for the devices to become queryable");
                await Task.Delay(100).ConfigureAwait(false);
                await enumerator.DisposeAsync().ConfigureAwait(false);
                enumerator = queryClient.CreateJobsQuery(options).AsPages().GetAsyncEnumerator();
                await enumerator.MoveNextAsync().ConfigureAwait(false);
            }

            await enumerator.DisposeAsync().ConfigureAwait(false);
        }

        private static async Task<string> ScheduleJobToBeQueriedAsync(ScheduledJobsClient jobsClient, string deviceId)
        {
            var twinUpdate = new ClientTwin();
            twinUpdate.Properties.Desired["key"] = "value";

            while (true)
            {
                try
                {
                    TwinScheduledJob scheduledJob = await jobsClient
                        .ScheduleTwinUpdateAsync("DeviceId IN ['" + deviceId + "']", twinUpdate, DateTimeOffset.UtcNow)
                        .ConfigureAwait(false);

                    return scheduledJob.JobId;
                }
                catch (IotHubServiceException ex) when (ex.StatusCode is (HttpStatusCode)429)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
        }
    }
}
